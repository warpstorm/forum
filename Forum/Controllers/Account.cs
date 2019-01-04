using Forum.Contexts;
using Forum.Enums;
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

		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IForumViewResult ForumViewResult { get; }
		ILogger Log { get; }

		public Account(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			UserManager<DataModels.ApplicationUser> userManager,
			IForumViewResult forumViewResult,
			ILogger<Account> log
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			UserManager = userManager;
			ForumViewResult = forumViewResult;
			Log = log;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new ViewModels.Account.IndexPage();

			foreach (var user in (await AccountRepository.Records()).Where(r => r.DisplayName != "Deleted Account")) {
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

			return await ForumViewResult.ViewResult(this, viewModel);
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
				FrontPage = userRecord.FrontPage,
				FrontPageOptions = FrontPagePickList(userRecord.FrontPage),
				MessagesPerPage = userRecord.MessagesPerPage,
				PopularityLimit = userRecord.PopularityLimit,
				Poseys = userRecord.Poseys,
				ShowFavicons = userRecord.ShowFavicons,
				TopicsPerPage = userRecord.TopicsPerPage
			};

			ModelState.Clear();

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Details(InputModels.UpdateAccountInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.UpdateAccount(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var userRecord = (await AccountRepository.Records()).First(item => item.Id == input.Id);

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
					FrontPage = userRecord.FrontPage,
					FrontPageOptions = FrontPagePickList(userRecord.FrontPage),
					MessagesPerPage = userRecord.MessagesPerPage,
					PopularityLimit = userRecord.PopularityLimit,
					Poseys = userRecord.Poseys,
					ShowFavicons = userRecord.ShowFavicons,
					TopicsPerPage = userRecord.TopicsPerPage
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateAvatar(InputModels.UpdateAvatarInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.UpdateAvatar(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var userRecord = input.Id is null ? UserContext.ApplicationUser : await UserManager.FindByIdAsync(input.Id);

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
					FrontPage = userRecord.FrontPage,
					FrontPageOptions = FrontPagePickList(userRecord.FrontPage),
					MessagesPerPage = userRecord.MessagesPerPage,
					PopularityLimit = userRecord.PopularityLimit,
					Poseys = userRecord.Poseys,
					ShowFavicons = userRecord.ShowFavicons,
					TopicsPerPage = userRecord.TopicsPerPage
				};

				return await ForumViewResult.ViewResult(this, nameof(Details), viewModel);
			}
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendVerificationEmail() {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.SendVerificationEmail();
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() => RedirectToAction(nameof(Account.Details), nameof(Account));
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ConfirmEmail(InputModels.ConfirmEmailInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ConfirmEmail(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() => await ForumViewResult.ViewResult(this);
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Login() {
			if (UserContext.IsAuthenticated) {
				return Redirect("/");
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.LoginPage();
			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha3]
		public async Task<IActionResult> Login(InputModels.LoginInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Login(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				if (UserContext.IsAuthenticated) {
					return Redirect("/");
				}

				await AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.LoginPage {
					Email = input.Email,
					RememberMe = input.RememberMe
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult AccessDenied() => View();

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Lockout() {
			await AccountRepository.SignOut();
			return await ForumViewResult.ViewResult(this);
		}

		[HttpGet]
		public async Task<IActionResult> Logout() {
			await AccountRepository.SignOut();
			return Redirect("/");
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Register() {
			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.RegisterPage {
				BirthdayDays = DayPickList(),
				BirthdayMonths = MonthPickList(),
				BirthdayYears = YearPickList()
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> Register(InputModels.RegisterInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Register(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				await AccountRepository.SignOut();

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

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword() {
			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ForgotPasswordPage();

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> ForgotPassword(InputModels.ForgotPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ForgotPassword(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				await AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.ForgotPasswordPage {
					Email = input.Email
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPasswordConfirmation() => View();

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(string code) {
			code.ThrowIfNull(nameof(code));

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ResetPasswordPage {
				Code = code
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha]
		public async Task<IActionResult> ResetPassword(InputModels.ResetPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.ResetPassword(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				await AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.ResetPasswordPage {
					Code = input.Code
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPasswordConfirmation() => await ForumViewResult.ViewResult(this);

		[HttpGet]
		public async Task<IActionResult> Delete(string userId) {
			var deletedAccount = (await AccountRepository.Records()).FirstOrDefault(item => item.DisplayName == "Deleted Account");

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

			return await ForumViewResult.ViewResult(this, "Delete", userId);
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmDelete(string userId) {
			if (UserContext.ApplicationUser.Id != userId && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			var deletedAccount = (await AccountRepository.Records()).FirstOrDefault(item => item.DisplayName == "Deleted Account");

			if (deletedAccount is null) {
				throw new HttpNotFoundError();
			}

			await AccountRepository.MergeAccounts(userId, deletedAccount.Id, true);

			return Redirect("/");
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Merge(string userId) {
			var viewModel = new ViewModels.Account.MergePage {
				SourceId = userId
			};

			foreach (var user in (await AccountRepository.Records()).Where(item => item.Id != userId && item.DisplayName != "Deleted Account")) {
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

			return await ForumViewResult.ViewResult(this, viewModel);
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

		public IEnumerable<SelectListItem> FrontPagePickList(EFrontPage selected) {
			return new List<SelectListItem> {
				new SelectListItem {
					Value = "0",
					Text = "Boards",
					Selected = selected == EFrontPage.Boards
				},
				new SelectListItem {
					Value = "1",
					Text = "All Topics",
					Selected = selected == EFrontPage.All
				},
				new SelectListItem {
					Value = "2",
					Text = "Unread Topics",
					Selected = selected == EFrontPage.Unread
				},
			};
		}
	}
}