using Forum.Controllers;
using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Models;
using Forum.Models.Errors;
using Forum.Services.Contexts;
using Forum.Services.Helpers;
using Forum.Services.Plugins.EmailSender;
using Forum.Services.Plugins.ImageStore;
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
using System.Reflection;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class AccountRepository : IRepository<DataModels.ApplicationUser> {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		IUrlHelper UrlHelper { get; }
		IEmailSender EmailSender { get; }
		IImageStore ImageStore { get; }
		ILogger<AccountRepository> Log { get; }

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
		) {
			DbContext = dbContext;
			UserContext = userContext;

			UserManager = userManager;
			SignInManager = signInManager;

			HttpContextAccessor = httpContextAccessor;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

			EmailSender = emailSender;
			ImageStore = imageStore;

			Log = log;
		}

		public async Task<List<DataModels.ApplicationUser>> Records() {
			if (_Records is null) {
				var records = await DbContext.Users.ToListAsync();
				_Records = records.OrderBy(r => r.DisplayName).ToList();

				var birthdayCakeImg = @"<img src=""/images/hbd.png"" alt=""Happy birthday!"" title=""Happy birthday!"" />";

				var onlineTimeLimit = DateTime.Now.AddMinutes(-5);
				var onlineChiclet = @"<span class=""whos-online-chiclet chiclet chiclet-green"" time=""{0}"" user=""{1}""></span>";
				var offlineChiclet = @"<span class=""whos-online-chiclet chiclet chiclet-gray"" time=""{0}"" user=""{1}""></span>";

				foreach (var user in _Records) {
					user.DecoratedName = string.Empty;

					var lastOnlineTime = user.LastOnline.ToHtmlLocalTimeString();
					var isOnline = user.LastOnline >= onlineTimeLimit;

					if (isOnline) {
						var personalizedChiclet = string.Format(onlineChiclet, lastOnlineTime, user.Id);
						user.DecoratedName += $"{personalizedChiclet} ";
					}
					else {
						var personalizedChiclet = string.Format(offlineChiclet, lastOnlineTime, user.Id);
						user.DecoratedName += $"{personalizedChiclet} ";
					}

					if (user.ShowBirthday) {
						var isBirthday = DateTime.Now.Date == new DateTime(DateTime.Now.Year, user.Birthday.Month, user.Birthday.Day).Date;

						if (isBirthday) {
							user.DecoratedName += $"{birthdayCakeImg} ";
						}
					}

					user.DecoratedName += user.DisplayName;
				}
			}

			return _Records;
		}
		List<DataModels.ApplicationUser> _Records;

		public async Task<List<ViewModels.Account.OnlineUser>> GetOnlineList() {
			// Users are considered "offline" after 5 minutes.
			var onlineTimeLimit = DateTime.Now.AddMinutes(-5);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsersQuery = from user in await Records()
								   where user.LastOnline >= onlineTodayTimeLimit
								   orderby user.LastOnline descending
								   select new {
									   user.Id,
									   user.DecoratedName,
									   user.LastOnline,
									   user.LastActionLogItemId
								   };

			var onlineUsers = new List<ViewModels.Account.OnlineUser>();

			foreach (var user in onlineUsersQuery) {
				var lastActionItem = await DbContext.ActionLog.FindAsync(user.LastActionLogItemId);

				onlineUsers.Add(new ViewModels.Account.OnlineUser {
					Id = user.Id,
					Name = user.DecoratedName,
					LastOnline = user.LastOnline,
					IsOnline = user.LastOnline > onlineTimeLimit,
					LastActionText = ActionLogItemText(lastActionItem),
					LastActionUrl = ActionLogItemUrl(lastActionItem)
				});
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
				serviceResponse.Error("Invalid login attempt.");
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

			await updateDisplayName();
			updateBirthday();
			updateFrontPage();
			updateMessagesPerPage();
			updateTopicsPerPage();
			updatePopularityLimit();
			updatePoseys();
			updateShowFavicons();

			if (serviceResponse.Success) {
				await DbContext.SaveChangesAsync();
			}

			await updateEmail();
			await updatePassword();

			return serviceResponse;

			async Task updateDisplayName() {
				if (serviceResponse.Success && input.DisplayName != userRecord.DisplayName) {
					if ((await Records()).Any(r => r.DisplayName == input.DisplayName)) {
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
					if (input.ShowBirthday != userRecord.ShowBirthday) {
						userRecord.ShowBirthday = input.ShowBirthday;
						DbContext.Update(userRecord);

						Log.LogInformation($"ShowBirthday was modified by '{UserContext.ApplicationUser.DisplayName}' for account '{userRecord.DisplayName}'.");
					}

					var birthday = new DateTime(userRecord.Registered.Year, input.BirthdayMonth, input.BirthdayDay);

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

							if (userRecord.Id == UserContext.ApplicationUser.Id) {
								await SignOut();
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
						await SignOut();
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

			if (!input.ConfirmThirteen) {
				var message = $"You must be 13 or older to register.";
				serviceResponse.Error(nameof(input.ConfirmThirteen), message);
				Log.LogWarning(message);
			}

			if ((await Records()).Any(r => r.DisplayName == input.DisplayName)) {
				var message = $"The display name '{input.DisplayName}' is already taken.";
				serviceResponse.Error(nameof(input.DisplayName), message);
				Log.LogWarning(message);
			}

			if (serviceResponse.Success) {
				var user = new DataModels.ApplicationUser {
					DisplayName = input.DisplayName,
					Registered = DateTime.Now,
					Birthday = DateTime.Now,
					LastOnline = DateTime.Now,
					UserName = input.Email,
					Email = input.Email
				};

				var identityResult = await UserManager.CreateAsync(user, input.Password);

				if (identityResult.Succeeded) {
					var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
					var callbackUrl = EmailConfirmationLink(user.Id, code);

					if (EmailSender.Ready) {
						await EmailSender.SendEmailConfirmationAsync(input.Email, callbackUrl);
					}

					var loginInput = new InputModels.LoginInput {
						Email = input.Email,
						Password = input.Password
					};

					return await Login(loginInput);
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

			await SignOut();

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
			var sourceAccount = (await Records()).First(item => item.Id == sourceId);
			var targetAccount = (await Records()).First(item => item.Id == targetId);

			var updateTasks = new List<Task>();

			var pSourceId = new SqlParameter("@SourceId", sourceId);
			var pTargetId = new SqlParameter("@TargetId", targetId);

			if (eraseContent) {
				updateTasks.AddRange(new List<Task> {
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Notifications)}] WHERE {nameof(DataModels.Notification.UserId)} = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Participants)}] WHERE {nameof(DataModels.Participant.UserId)} = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Bookmarks)}] WHERE {nameof(DataModels.Bookmark.UserId)} = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.ViewLogs)}] WHERE {nameof(DataModels.ViewLog.UserId)} = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Quotes)}] WHERE {nameof(DataModels.Quote.PostedById)} = @SourceId", pSourceId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.EditedById)} = @TargetId WHERE {nameof(DataModels.Message.EditedById)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.LastReplyById)} = @TargetId WHERE {nameof(DataModels.Message.LastReplyById)} = @SourceId", pSourceId, pTargetId),
				});

				updateTasks.Add(DbContext.Database.ExecuteSqlCommandAsync($@"
					UPDATE [{nameof(ApplicationDbContext.Messages)}] SET
						{nameof(DataModels.Message.PostedById)} = @TargetId,
						{nameof(DataModels.Message.OriginalBody)} = '',
						{nameof(DataModels.Message.DisplayBody)} = 'This account has been deleted.',
						{nameof(DataModels.Message.LongPreview)} = '',
						{nameof(DataModels.Message.ShortPreview)} = '',
						{nameof(DataModels.Message.Cards)} = ''
					WHERE {nameof(DataModels.Message.PostedById)} = @SourceId", pSourceId, pTargetId));
			}
			else {
				updateTasks.AddRange(new List<Task> {
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Notifications)}] SET {nameof(DataModels.Notification.UserId)} = @TargetId WHERE {nameof(DataModels.Notification.UserId)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Participants)}] SET {nameof(DataModels.Participant.UserId)} = @TargetId WHERE {nameof(DataModels.Participant.UserId)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Bookmarks)}] SET {nameof(DataModels.Bookmark.UserId)} = @TargetId WHERE {nameof(DataModels.Bookmark.UserId)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.ViewLogs)}] SET {nameof(DataModels.ViewLog.UserId)} = @TargetId WHERE {nameof(DataModels.ViewLog.UserId)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Quotes)}] SET {nameof(DataModels.Quote.PostedById)} = @TargetId WHERE {nameof(DataModels.Quote.PostedById)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.PostedById)} = @TargetId WHERE {nameof(DataModels.Message.PostedById)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.EditedById)} = @TargetId WHERE {nameof(DataModels.Message.EditedById)} = @SourceId", pSourceId, pTargetId),
					DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.LastReplyById)} = @TargetId WHERE {nameof(DataModels.Message.LastReplyById)} = @SourceId", pSourceId, pTargetId),
				});
			}

			updateTasks.AddRange(new List<Task> {
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.MessageBoards)}] SET {nameof(DataModels.MessageBoard.UserId)} = @TargetId WHERE {nameof(DataModels.MessageBoard.UserId)} = @SourceId", pSourceId, pTargetId),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.MessageThoughts)}] SET {nameof(DataModels.MessageThought.UserId)} = @TargetId WHERE {nameof(DataModels.MessageThought.UserId)} = @SourceId", pSourceId, pTargetId),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Notifications)}] SET {nameof(DataModels.Notification.TargetUserId)} = @TargetId WHERE {nameof(DataModels.Notification.TargetUserId)} = @SourceId", pSourceId, pTargetId),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Quotes)}] SET {nameof(DataModels.Quote.SubmittedById)} = @TargetId WHERE {nameof(DataModels.Quote.SubmittedById)} = @SourceId", pSourceId, pTargetId),
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

		public async Task SignOut() {
			HttpContextAccessor.HttpContext.Session.Remove(Constants.InternalKeys.UserId);

			await Task.WhenAll(new[] {
				HttpContextAccessor.HttpContext.Session.CommitAsync(),
				SignInManager.SignOutAsync()
			});
		}

		public string ActionLogItemText(DataModels.ActionLogItem logItem) {
			if (!(logItem is null)) {
				var controller = Type.GetType($"Forum.Controllers.{logItem.Controller}");

				try {
					var action = controller.GetMethod(logItem.Action);

					var attribute = action.GetCustomAttributes(typeof(ActionLogAttribute), false).FirstOrDefault() as ActionLogAttribute;

					if (!(attribute is null)) {
						return attribute.Description;
					}
				}
				catch (AmbiguousMatchException) { }
			}

			return string.Empty;
		}

		public string ActionLogItemUrl(DataModels.ActionLogItem logItem) => logItem is null ? string.Empty : UrlHelper.Action(logItem.Action, logItem.Controller, logItem.Arguments);
		public string EmailConfirmationLink(string userId, string code) => UrlHelper.AbsoluteAction(nameof(Account.ConfirmEmail), nameof(Account), new { userId, code });
		public string ResetPasswordCallbackLink(string userId, string code) => UrlHelper.AbsoluteAction(nameof(Account.ResetPassword), nameof(Account), new { userId, code });
	}
}