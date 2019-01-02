using Forum.Contexts;
using Forum.Errors;
using Forum.Extensions;
using Forum.Interfaces.Services;
using Forum.Plugins.Recaptcha;
using Forum.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Account : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		AccountRepository AccountRepository { get; }
		SettingsRepository SettingsRepository { get; }

		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IForumViewResult ForumViewResult { get; }
		ILogger Log { get; }

		public Account(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			SettingsRepository settingsRepository,
			UserManager<DataModels.ApplicationUser> userManager,
			IForumViewResult forumViewResult,
			ILogger<Account> log
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			SettingsRepository = settingsRepository;
			UserManager = userManager;
			ForumViewResult = forumViewResult;
			Log = log;
		}

		[HttpGet]
		public IActionResult Index() {
			var viewModel = new ViewModels.Account.IndexPage();

			foreach (var user in AccountRepository.Where(r => r.DisplayName != "Deleted Account")) {
				var indexItem = new ViewModels.Account.IndexItem {
					Id = user.Id,
					DisplayName = user.DisplayName,
					Email = user.Email,
					Registered = user.Registered.ToPassedTimeString(),
					LastOnline = user.LastOnline.ToPassedTimeString(),
					CanManage = UserContext.IsAdmin || user.Id == UserContext.ApplicationUser.Id
				};

				viewModel.IndexItems.Add(indexItem);
			}

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Details(string id) {
			var userRecord = id is null ? UserContext.ApplicationUser : await UserManager.FindByIdAsync(id);

			if (userRecord is null) {
				userRecord = UserContext.ApplicationUser;
			}

			AccountRepository.CanEdit(userRecord.Id);

			var viewModel = new ViewModels.Account.DetailsPage {
				AvatarPath = userRecord.AvatarPath,
				Id = userRecord.Id,
				DisplayName = userRecord.DisplayName,
				NewEmail = userRecord.Email,
				EmailConfirmed = userRecord.EmailConfirmed,
				BirthdayDays = DayPickList(userRecord.Birthday.Day),
				BirthdayMonths = MonthPickList(userRecord.Birthday.Month),
				BirthdayYears = YearPickList(userRecord.Birthday.Year),
				BirthdayDay = userRecord.Birthday.Day.ToString(),
				BirthdayMonth = userRecord.Birthday.Month.ToString(),
				BirthdayYear = userRecord.Birthday.Year.ToString(),
				Settings = SettingsRepository.GetUserSettingsList(userRecord.Id)
			};

			ModelState.Clear();

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Details(InputModels.UpdateAccountInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.UpdateAccount(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				var userRecord = AccountRepository.First(item => item.Id == input.Id);

				AccountRepository.CanEdit(userRecord.Id);

				var viewModel = new ViewModels.Account.DetailsPage {
					DisplayName = input.DisplayName,
					NewEmail = input.NewEmail,
					AvatarPath = userRecord.AvatarPath,
					Id = userRecord.Id,
					EmailConfirmed = userRecord.EmailConfirmed,
					BirthdayDays = DayPickList(input.BirthdayDay),
					BirthdayMonths = MonthPickList(input.BirthdayMonth),
					BirthdayYears = YearPickList(input.BirthdayYear),
					BirthdayDay = input.BirthdayDay.ToString(),
					BirthdayMonth = input.BirthdayMonth.ToString(),
					BirthdayYear = input.BirthdayYear.ToString(),
					Settings = SettingsRepository.GetUserSettingsList(userRecord.Id)
				};

				return ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateAvatar(InputModels.UpdateAvatarInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.UpdateAvatar(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				var userRecord = input.Id is null ? UserContext.ApplicationUser : UserManager.FindByIdAsync(input.Id).Result;

				if (userRecord is null) {
					userRecord = UserContext.ApplicationUser;
				}

				AccountRepository.CanEdit(userRecord.Id);

				var viewModel = new ViewModels.Account.DetailsPage {
					AvatarPath = userRecord.AvatarPath,
					Id = userRecord.Id,
					DisplayName = userRecord.DisplayName,
					NewEmail = userRecord.Email,
					EmailConfirmed = userRecord.EmailConfirmed,
					BirthdayDays = DayPickList(userRecord.Birthday.Day),
					BirthdayMonths = MonthPickList(userRecord.Birthday.Month),
					BirthdayYears = YearPickList(userRecord.Birthday.Year),
					BirthdayDay = userRecord.Birthday.Day.ToString(),
					BirthdayMonth = userRecord.Birthday.Month.ToString(),
					BirthdayYear = userRecord.Birthday.Year.ToString(),
					Settings = SettingsRepository.GetUserSettingsList(userRecord.Id)
				};

				return ForumViewResult.ViewResult(this, nameof(Details), viewModel);
			}
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendVerificationEmail() {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.SendVerificationEmail();
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return RedirectToAction(nameof(Profile.Details), nameof(Profile));
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ConfirmEmail(InputModels.ConfirmEmailInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ConfirmEmail(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return ForumViewResult.ViewResult(this);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Login() {
			if (AccountRepository.IsAuthenticated) {
				return RedirectToAction(nameof(Home.FrontPage), nameof(Home));
			}

			AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.LoginPage();
			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> Login(InputModels.LoginInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Login(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				if (AccountRepository.IsAuthenticated) {
					return RedirectToAction(nameof(Home.FrontPage), nameof(Home));
				}

				AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.LoginPage {
					Email = input.Email,
					RememberMe = input.RememberMe
				};

				return ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult AccessDenied() => View();

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Lockout() {
			AccountRepository.SignOut();
			return ForumViewResult.ViewResult(this);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Logout() {
			AccountRepository.SignOut();
			return RedirectToAction(nameof(Home.FrontPage), nameof(Home));
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Register() {
			AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.RegisterPage {
				BirthdayDays = DayPickList(),
				BirthdayMonths = MonthPickList(),
				BirthdayYears = YearPickList()
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> Register(InputModels.RegisterInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Register(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.RegisterPage {
					BirthdayDays = DayPickList(),
					BirthdayDay = input.BirthdayDay.ToString(),
					BirthdayMonths = MonthPickList(),
					BirthdayMonth = input.BirthdayMonth.ToString(),
					BirthdayYears = YearPickList(),
					BirthdayYear = input.BirthdayYear.ToString(),
					DisplayName = input.DisplayName,
					Email = input.Email,
					ConfirmEmail = input.ConfirmEmail,
					Password = input.Password,
					ConfirmPassword = input.ConfirmPassword,
				};

				return ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPassword() {
			AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ForgotPasswordPage();

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> ForgotPassword(InputModels.ForgotPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ForgotPassword(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.ForgotPasswordPage {
					Email = input.Email
				};

				return ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPasswordConfirmation() => View();

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPassword(string code) {
			code.ThrowIfNull(nameof(code));

			AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ResetPasswordPage {
				Code = code
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> ResetPassword(InputModels.ResetPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ResetPassword(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.ResetPasswordPage {
					Code = input.Code
				};

				return ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPasswordConfirmation() => ForumViewResult.ViewResult(this);

		[HttpGet]
		public IActionResult Delete(string userId) {
			var deletedAccount = AccountRepository.FirstOrDefault(item => item.DisplayName == "Deleted Account");

			if (deletedAccount is null) {
				deletedAccount = new DataModels.ApplicationUser {
					DisplayName = "Deleted Account",
					UserName = Guid.NewGuid().ToString(),
					Email = Guid.NewGuid().ToString(),
					AvatarPath = string.Empty,
					Birthday = new DateTime(2000, 1, 1),
					Registered = new DateTime(2000, 1, 1),
				};

				DbContext.Users.Add(deletedAccount);
				DbContext.SaveChanges();
			}

			return ForumViewResult.ViewResult(this, "Delete", userId);
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmDelete(string userId) {
			if (UserContext.ApplicationUser.Id != userId && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			var deletedAccount = AccountRepository.FirstOrDefault(item => item.DisplayName == "Deleted Account");

			if (deletedAccount is null) {
				throw new HttpNotFoundError();
			}

			await AccountRepository.MergeAccounts(userId, deletedAccount.Id, true);

			return RedirectToAction(nameof(Home.FrontPage), nameof(Home));
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public IActionResult Merge(string userId) {
			var viewModel = new ViewModels.Account.MergePage {
				SourceId = userId
			};

			foreach (var user in AccountRepository.Where(item => item.Id != userId && item.DisplayName != "Deleted Account")) {
				var indexItem = new ViewModels.Account.IndexItem {
					Id = user.Id,
					DisplayName = user.DisplayName,
					Email = user.Email,
					Registered = user.Registered.ToPassedTimeString(),
					LastOnline = user.LastOnline.ToPassedTimeString(),
					CanManage = UserContext.IsAdmin || user.Id == UserContext.ApplicationUser.Id
				};

				viewModel.IndexItems.Add(indexItem);
			}

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> ConfirmMerge(string sourceId, string targetId) {
			await AccountRepository.MergeAccounts(sourceId, targetId, false);

			return RedirectToAction(nameof(Account.Details), nameof(Account), new { id = targetId });
		}

		public IEnumerable<SelectListItem> YearPickList(int selected = -1) {
			var years = from number in Enumerable.Range(1900, DateTime.Now.Year - 1900)
						orderby number descending
						select new SelectListItem {
							Value = number.ToString(),
							Text = number.ToString(),
							Selected = selected > -1 && number == selected
						};

			years.Prepend(new SelectListItem {
				Disabled = true,
				Text = "Year"
			});

			return years;
		}

		public IEnumerable<SelectListItem> DayPickList(int selected = -1) {
			var days = from number in Enumerable.Range(1, 31)
					   select new SelectListItem {
						   Value = number.ToString(),
						   Text = number.ToString(),
						   Selected = selected > -1 && number == selected
					   };

			days.Prepend(new SelectListItem {
				Disabled = true,
				Text = "Day"
			});

			return days;
		}

		public IEnumerable<SelectListItem> MonthPickList(int selected = -1) {
			var months = from number in Enumerable.Range(1, 12)
						 select new SelectListItem {
							 Value = number.ToString(),
							 Text = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(number),
							 Selected = selected > -1 && number == selected
						 };

			months.Prepend(new SelectListItem {
				Disabled = true,
				Text = "Month"
			});

			return months;
		}
	}
}