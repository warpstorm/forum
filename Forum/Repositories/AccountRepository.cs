using Forum.Contexts;
using Forum.Controllers;
using Forum.Errors;
using Forum.Extensions;
using Forum.Plugins.EmailSender;
using Forum.Plugins.ImageStore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class AccountRepository : Repository<DataModels.ApplicationUser> {
		public bool IsAuthenticated => UserContext.IsAuthenticated;

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		IUrlHelper UrlHelper { get; }
		IEmailSender EmailSender { get; }
		IImageStore ImageStore { get; }

		public AccountRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			UserManager<DataModels.ApplicationUser> userManager,
			SignInManager<DataModels.ApplicationUser> signInManager,
			IHttpContextAccessor httpContextAccessor,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory,
			IEmailSender emailSender,
			IImageStore imageStore,
			ILogger<AccountRepository> log
		) : base(log) {
			DbContext = dbContext;
			UserContext = userContext;

			UserManager = userManager;
			SignInManager = signInManager;

			HttpContextAccessor = httpContextAccessor;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

			EmailSender = emailSender;
			ImageStore = imageStore;
		}

		public List<ViewModels.Profile.OnlineUser> GetOnlineList() {
			// Users are considered "offline" after 5 minutes.
			var onlineTimeLimit = DateTime.Now.AddMinutes(5);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsersQuery = from user in Records
								   where user.LastOnline >= onlineTodayTimeLimit
								   orderby user.LastOnline descending
								   select new ViewModels.Profile.OnlineUser {
									   Id = user.Id,
									   Name = user.DisplayName,
									   Online = user.LastOnline >= onlineTimeLimit,
									   Birthday = user.Birthday,
									   LastOnline = user.LastOnline
								   };

			var onlineUsers = onlineUsersQuery.ToList();
			var birthdayUsers = onlineUsers.Where(u => DateTime.Now.Date == new DateTime(DateTime.Now.Year, u.Birthday.Month, u.Birthday.Day).Date);

			foreach (var user in birthdayUsers) {
				user.IsBirthday = true;
			}

			return onlineUsers;
		}

		public async Task<ServiceModels.ServiceResponse> Login(InputModels.LoginInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var result = await SignInManager.PasswordSignInAsync(input.Email, input.Password, input.RememberMe, lockoutOnFailure: false);

			if (result.IsLockedOut) {
				Log.LogWarning($"User account locked out '{input.Email}'.");
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Account.Lockout), nameof(Account));
			}
			else if (!result.Succeeded) {
				Log.LogWarning($"Invalid login attempt for account '{input.Email}'.");

				var user = Records.FirstOrDefault(u => u.Email == input.Email);

				if (user != null && !user.EmailConfirmed) {
					serviceResponse.Error("Your account isn't activated. Check your email for the link. Check your spam folder if you didn't get the message after 5 minutes.");
				}
				else {
					serviceResponse.Error("Invalid login attempt.");
				}
			}
			else {
				Log.LogInformation($"User logged in '{input.Email}'.");
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> UpdateAccount(InputModels.UpdateAccountInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (!await UserManager.CheckPasswordAsync(UserContext.ApplicationUser, input.Password)) {
				var message = $"Invalid password for '{input.DisplayName}'.";
				serviceResponse.Error(nameof(InputModels.UpdateAccountInput.Password), message);
				Log.LogWarning(message);
			}

			var userRecord = await UserManager.FindByIdAsync(input.Id);

			if (userRecord is null) {
				var message = $"No user record found for '{input.DisplayName}'.";
				serviceResponse.Error(message);
				Log.LogCritical(message);
			}

			CanEdit(userRecord.Id);

			updateDisplayName();
			updateBirthday();
			updateFrontPage();
			updateMessagesPerPage();
			updateTopicsPerPage();
			updatePopularityLimit();
			updatePoseys();
			updateShowFavicons();

			if (serviceResponse.Success) {
				DbContext.SaveChanges();
			}

			await updateEmail();
			await updatePassword();

			return serviceResponse;

			void updateDisplayName() {
				if (serviceResponse.Success && input.DisplayName != userRecord.DisplayName) {
					if (Records.Any(r => r.DisplayName == input.DisplayName)) {
						var message = $"The display name '{input.DisplayName}' is already taken.";
						serviceResponse.Error(message);
						Log.LogWarning(message);
					}
					else {
						userRecord.DisplayName = input.DisplayName;
						DbContext.Update(userRecord);

						Log.LogInformation($"Display name was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
					}
				}
			}

			void updateBirthday() {
				if (serviceResponse.Success) {
					var birthday = new DateTime(input.BirthdayYear, input.BirthdayMonth, input.BirthdayDay);

					if (birthday != userRecord.Birthday) {
						userRecord.Birthday = birthday;
						DbContext.Update(userRecord);

						Log.LogInformation($"Birthday was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
					}
				}
			}

			void updateFrontPage() {
				if (serviceResponse.Success && input.FrontPage != userRecord.FrontPage) {
					userRecord.FrontPage = input.FrontPage;
					DbContext.Update(userRecord);

					Log.LogInformation($"FrontPage was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
				}
			}

			void updateMessagesPerPage() {
				if (serviceResponse.Success && input.MessagesPerPage != userRecord.MessagesPerPage) {
					userRecord.MessagesPerPage = input.MessagesPerPage;
					DbContext.Update(userRecord);

					Log.LogInformation($"MessagesPerPage was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
				}
			}

			void updateTopicsPerPage() {
				if (serviceResponse.Success && input.TopicsPerPage != userRecord.TopicsPerPage) {
					userRecord.TopicsPerPage = input.TopicsPerPage;
					DbContext.Update(userRecord);

					Log.LogInformation($"TopicsPerPage was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
				}
			}

			void updatePopularityLimit() {
				if (serviceResponse.Success && input.PopularityLimit != userRecord.PopularityLimit) {
					userRecord.PopularityLimit = input.PopularityLimit;
					DbContext.Update(userRecord);

					Log.LogInformation($"PopularityLimit was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
				}
			}

			void updatePoseys() {
				if (serviceResponse.Success && input.Poseys != userRecord.Poseys) {
					userRecord.Poseys = input.Poseys;
					DbContext.Update(userRecord);

					Log.LogInformation($"Poseys was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
				}
			}

			void updateShowFavicons() {
				if (serviceResponse.Success && input.ShowFavicons != userRecord.ShowFavicons) {
					userRecord.ShowFavicons = input.ShowFavicons;
					DbContext.Update(userRecord);

					Log.LogInformation($"ShowFavicons was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
				}
			}

			async Task updateEmail() {
				if (input.NewEmail != userRecord.Email) {
					serviceResponse.RedirectPath = UrlHelper.Action(nameof(Account.Details), nameof(Account), new { id = input.DisplayName });

					var identityResult = await UserManager.SetEmailAsync(userRecord, input.NewEmail);

					if (!identityResult.Succeeded) {
						foreach (var error in identityResult.Errors) {
							Log.LogError($"Error modifying email by '{UserContext.ApplicationUser.DisplayName}' from '{userRecord.Email}' to '{input.NewEmail}'. Message: {error.Description}");
							serviceResponse.Error(error.Description);
						}
					}
					else {
						Log.LogInformation($"Email address was modified by '{UserContext.ApplicationUser.DisplayName}' from '{userRecord.Email}' to '{input.NewEmail}'.");

						identityResult = await UserManager.SetUserNameAsync(userRecord, input.NewEmail);

						if (!identityResult.Succeeded) {
							foreach (var error in identityResult.Errors) {
								Log.LogError($"Error modifying username by '{UserContext.ApplicationUser.DisplayName}' from '{userRecord.Email}' to '{input.NewEmail}'. Message: {error.Description}");
								serviceResponse.Error(error.Description);
							}
						}
						else {
							Log.LogInformation($"Username was modified by '{UserContext.ApplicationUser.DisplayName}' from '{userRecord.Email}' to '{input.NewEmail}'.");

							var code = await UserManager.GenerateEmailConfirmationTokenAsync(userRecord);

							if (EmailSender.Ready) {
								var callbackUrl = EmailConfirmationLink(userRecord.Id, code);

								await EmailSender.SendEmailConfirmationAsync(input.NewEmail, callbackUrl);

								if (userRecord.Id == UserContext.ApplicationUser.Id) {
									SignOut();
								}
							}
							else {
								identityResult = await UserManager.ConfirmEmailAsync(userRecord, code);

								if (!identityResult.Succeeded) {
									foreach (var error in identityResult.Errors) {
										Log.LogError($"Error confirming '{userRecord.Email}'. Message: {error.Description}");
										serviceResponse.Error(error.Description);
									}
								}
								else {
									Log.LogInformation($"User confirmed email '{userRecord.Email}'.");
								}
							}
						}
					}
				}
			}

			async Task updatePassword() {
				if (!string.IsNullOrEmpty(input.NewPassword) && input.Password != input.NewPassword && UserContext.ApplicationUser.Id == input.Id) {
					var identityResult = await UserManager.ChangePasswordAsync(userRecord, input.Password, input.NewPassword);

					if (!identityResult.Succeeded) {
						foreach (var error in identityResult.Errors) {
							Log.LogError($"Error modifying password by '{UserContext.ApplicationUser.DisplayName}' for '{userRecord.DisplayName}'. Message: {error.Description}");
							serviceResponse.Error(nameof(InputModels.UpdateAccountInput.NewPassword), error.Description);
						}
					}
					else if (userRecord.Id == UserContext.ApplicationUser.Id) {
						Log.LogInformation($"Password was modified by '{UserContext.ApplicationUser.DisplayName}' for '{userRecord.DisplayName}'.");
						SignOut();
					}
				}
			}
		}

		public async Task<ServiceModels.ServiceResponse> UpdateAvatar(InputModels.UpdateAvatarInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var userRecord = await UserManager.FindByIdAsync(input.Id);

			if (userRecord is null) {
				var message = $"No user record found for '{input.Id}'.";
				serviceResponse.Error(message);
				Log.LogCritical(message);
			}

			CanEdit(input.Id);

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var allowedExtensions = new[] { ".gif", ".jpg", ".png", ".jpeg" };

			var extension = Path.GetExtension(input.NewAvatar.FileName).ToLower();

			if (!allowedExtensions.Contains(extension)) {
				serviceResponse.Error(nameof(input.NewAvatar), "Your avatar must end with .gif, .jpg, .jpeg, or .png");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			using (var inputStream = input.NewAvatar.OpenReadStream()) {
				inputStream.Position = 0;

				userRecord.AvatarPath = await ImageStore.Save(new ImageStoreSaveOptions {
					ContainerName = Constants.InternalKeys.AvatarContainer,
					FileName = $"avatar{userRecord.Id}.png",
					ContentType = "image/png",
					InputStream = inputStream,
					MaxDimension = 100,
					Overwrite = true
				});
			}

			DbContext.Update(userRecord);
			DbContext.SaveChanges();

			Log.LogInformation($"Avatar was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Register(InputModels.RegisterInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var birthday = new DateTime(input.BirthdayYear, input.BirthdayMonth, input.BirthdayDay);

			if ((DateTime.Now - birthday).TotalDays < 13 * 365) {
				var message = $"You must be 13 or older to register.";
				serviceResponse.Error(message);
				Log.LogWarning(message);
			}

			if (Records.Any(r => r.DisplayName == input.DisplayName)) {
				var message = $"The display name '{input.DisplayName}' is already taken.";
				serviceResponse.Error(message);
				Log.LogWarning(message);
			}

			if (serviceResponse.Success) {
				var user = new DataModels.ApplicationUser {
					DisplayName = input.DisplayName,
					Registered = DateTime.Now,
					LastOnline = DateTime.Now,
					UserName = input.Email,
					Email = input.Email,
					Birthday = birthday
				};

				var identityResult = await UserManager.CreateAsync(user, input.Password);

				if (identityResult.Succeeded) {
					Log.LogInformation($"User created a new account with password '{input.Email}'.");

					var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
					var callbackUrl = EmailConfirmationLink(user.Id, code);

					if (EmailSender.Ready) {
						await EmailSender.SendEmailConfirmationAsync(input.Email, callbackUrl);
						serviceResponse.Message = "Please check your email for the activation link.";
					}
					else {
						serviceResponse.RedirectPath = callbackUrl;
					}
				}
				else {
					foreach (var error in identityResult.Errors) {
						Log.LogError($"Error registering '{input.Email}'. Message: {error.Description}");
						serviceResponse.Error(error.Description);
					}
				}
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> SendVerificationEmail() {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var code = await UserManager.GenerateEmailConfirmationTokenAsync(UserContext.ApplicationUser);
			var callbackUrl = EmailConfirmationLink(UserContext.ApplicationUser.Id, code);
			var email = UserContext.ApplicationUser.Email;

			if (!EmailSender.Ready) {
				serviceResponse.RedirectPath = callbackUrl;
				return serviceResponse;
			}

			await EmailSender.SendEmailConfirmationAsync(email, callbackUrl);

			serviceResponse.Message = "Verification email sent. Please check your email.";
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> ConfirmEmail(InputModels.ConfirmEmailInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var account = await UserManager.FindByIdAsync(input.UserId);

			if (account is null) {
				serviceResponse.Error($"Unable to load account '{input.UserId}'.");
			}

			if (serviceResponse.Success) {
				var identityResult = await UserManager.ConfirmEmailAsync(account, input.Code);

				if (!identityResult.Succeeded) {
					foreach (var error in identityResult.Errors) {
						Log.LogError($"Error confirming '{account.Email}'. Message: {error.Description}");
						serviceResponse.Error(error.Description);
					}
				}
				else {
					Log.LogInformation($"User confirmed email '{account.Id}'.");
				}
			}

			SignOut();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> ForgotPassword(InputModels.ForgotPasswordInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var account = await UserManager.FindByNameAsync(input.Email);

			if (account != null && await UserManager.IsEmailConfirmedAsync(account)) {
				var code = await UserManager.GeneratePasswordResetTokenAsync(account);
				var callbackUrl = ResetPasswordCallbackLink(account.Id, code);

				if (!EmailSender.Ready) {
					serviceResponse.RedirectPath = callbackUrl;
					return serviceResponse;
				}

				await EmailSender.SendEmailAsync(input.Email, "Reset Password", $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Account.ForgotPasswordConfirmation));
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> ResetPassword(InputModels.ResetPasswordInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var account = await UserManager.FindByEmailAsync(input.Email);

			if (account != null) {
				var identityResult = await UserManager.ResetPasswordAsync(account, input.Code, input.Password);

				if (!identityResult.Succeeded) {
					foreach (var error in identityResult.Errors) {
						Log.LogError($"Error resetting password for '{account.Email}'. Message: {error.Description}");
					}
				}
				else {
					Log.LogInformation($"Password was reset for '{account.Email}'.");
				}
			}

			serviceResponse.RedirectPath = nameof(Account.ResetPasswordConfirmation);
			return serviceResponse;
		}

		public async Task MergeAccounts(string sourceId, string targetId, bool eraseContent) {
			var sourceAccount = First(item => item.Id == sourceId);
			var targetAccount = First(item => item.Id == targetId);

			var updateTasks = new List<Task>();

			var pSourceId = new SqlParameter("@SourceId", sourceId);
			var pTargetId = new SqlParameter("@TargetId", targetId);

			if (eraseContent) {
				updateTasks.AddRange(new List<Task> {
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [Notifications] WHERE UserId = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [Participants] WHERE UserId = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [Pins] WHERE UserId = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [ViewLogs] WHERE UserId = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [Quotes] WHERE PostedById = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Messages] SET EditedById = @TargetId WHERE EditedById = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Messages] SET LastReplyById = @TargetId WHERE LastReplyById = @SourceId", pSourceId, pTargetId),
				});

				updateTasks.Add(DbContext.Database.ExecuteSqlCommandAsync($@"
					UPDATE [Messages] SET
						PostedById = @TargetId,
						OriginalBody = '',
						DisplayBody = 'This account has been deleted.',
						LongPreview = '',
						ShortPreview = '',
						Cards = ''
					WHERE PostedById = @SourceId", pSourceId, pTargetId));
			}
			else {
				updateTasks.AddRange(new List<Task> {
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Notifications] SET UserId = @TargetId WHERE UserId = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Participants] SET UserId = @TargetId WHERE UserId = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Pins] SET UserId = @TargetId WHERE UserId = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [ViewLogs] SET UserId = @TargetId WHERE UserId = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Quotes] SET PostedById = @TargetId WHERE PostedById = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Messages] SET PostedById = @TargetId WHERE PostedById = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Messages] SET EditedById = @TargetId WHERE EditedById = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Messages] SET LastReplyById = @TargetId WHERE LastReplyById = @SourceId", pSourceId, pTargetId),
				});
			}

			updateTasks.AddRange(new List<Task> {
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [MessageBoards] SET UserId = @TargetId WHERE UserId = @SourceId", pSourceId, pTargetId),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [MessageThoughts] SET UserId = @TargetId WHERE UserId = @SourceId", pSourceId, pTargetId),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Notifications] SET TargetUserId = @TargetId WHERE TargetUserId = @SourceId", pSourceId, pTargetId),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [Quotes] SET SubmittedById = @TargetId WHERE SubmittedById = @SourceId", pSourceId, pTargetId),
			});

			Task.WaitAll(updateTasks.ToArray());

			DbContext.SaveChanges();

			await UserManager.DeleteAsync(sourceAccount);

			DbContext.SaveChanges();
		}

		public void CanEdit(string userId) {
			if (userId == UserContext.ApplicationUser.Id || UserContext.IsAdmin) {
				return;
			}

			Log.LogWarning($"A user tried to edit another user's profile. {UserContext.ApplicationUser.DisplayName}");

			throw new HttpForbiddenError();
		}

		public void SignOut() {
			HttpContextAccessor.HttpContext.Session.Remove(Constants.InternalKeys.UserId);

			Task.WaitAll(new[] {
				HttpContextAccessor.HttpContext.Session.CommitAsync(),
				SignInManager.SignOutAsync()
			});
		}

		public string EmailConfirmationLink(string userId, string code) => UrlHelper.AbsoluteAction(nameof(Account.ConfirmEmail), nameof(Account), new { userId, code });
		public string ResetPasswordCallbackLink(string userId, string code) => UrlHelper.AbsoluteAction(nameof(Account.ResetPassword), nameof(Account), new { userId, code });
		protected override List<DataModels.ApplicationUser> GetRecords() => DbContext.Users.ToList().OrderBy(u => u.DisplayName).ToList();
	}
}