using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Models.Errors;
using Forum.Models.Options;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Plugins.Recaptcha;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
		ForumViewResult ForumViewResult { get; }

		public Account(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			UserManager<DataModels.ApplicationUser> userManager,
			ForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			UserManager = userManager;
			ForumViewResult = forumViewResult;
		}

		[ActionLog("is viewing the user list.")]
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

			return View(viewModel);
		}

		[ActionLog("is viewing a profile.")]
		[HttpGet]
		public async Task<IActionResult> Details(string id = "") {
			var userRecord = string.IsNullOrEmpty(id) ? UserContext.ApplicationUser : await UserManager.FindByIdAsync(id);

			if (userRecord is null) {
				userRecord = UserContext.ApplicationUser;
			}

			AccountRepository.CanEdit(userRecord.Id);

			var viewModel = new ViewModels.Account.DetailsPage {
				AvatarPath = userRecord.AvatarPath,
				Id = userRecord.Id,
				DisplayName = userRecord.DisplayName,
				ImgurName = userRecord.ImgurName,
				NewEmail = userRecord.Email,
				EmailConfirmed = userRecord.EmailConfirmed,
				BirthdayDays = DayPickList(userRecord.Birthday.Day),
				BirthdayMonths = MonthPickList(userRecord.Birthday.Month),
				BirthdayDay = userRecord.Birthday.Day.ToString(),
				BirthdayMonth = userRecord.Birthday.Month.ToString(),
				FrontPage = userRecord.FrontPage,
				FrontPageOptions = FrontPagePickList(userRecord.FrontPage),
				MessagesPerPage = userRecord.MessagesPerPage,
				PopularityLimit = userRecord.PopularityLimit,
				Poseys = userRecord.Poseys,
				ShowFavicons = userRecord.ShowFavicons ?? true,
				TopicsPerPage = userRecord.TopicsPerPage,
				ShowBirthday = userRecord.ShowBirthday
			};

			ModelState.Clear();

			return View(viewModel);
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
					BirthdayDay = input.BirthdayDay.ToString(),
					BirthdayMonth = input.BirthdayMonth.ToString(),
					FrontPage = userRecord.FrontPage,
					FrontPageOptions = FrontPagePickList(userRecord.FrontPage),
					MessagesPerPage = userRecord.MessagesPerPage,
					PopularityLimit = userRecord.PopularityLimit,
					Poseys = userRecord.Poseys,
					ShowFavicons = userRecord.ShowFavicons ?? true,
					TopicsPerPage = userRecord.TopicsPerPage
				};

				return View(viewModel);
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
					BirthdayDay = userRecord.Birthday.Day.ToString(),
					BirthdayMonth = userRecord.Birthday.Month.ToString(),
					FrontPage = userRecord.FrontPage,
					FrontPageOptions = FrontPagePickList(userRecord.FrontPage),
					MessagesPerPage = userRecord.MessagesPerPage,
					PopularityLimit = userRecord.PopularityLimit,
					Poseys = userRecord.Poseys,
					ShowFavicons = userRecord.ShowFavicons ?? true,
					TopicsPerPage = userRecord.TopicsPerPage
				};

				return View(nameof(Details), viewModel);
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
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: View);
			}

			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Login() {
			if (UserContext.IsAuthenticated) {
				return Redirect("/");
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.LoginPage();
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha3]
		public async Task<IActionResult> Login(InputModels.LoginInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Login(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return RedirectToAction(nameof(LoginClassic));
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> LoginClassic() {
			if (UserContext.IsAuthenticated) {
				return Redirect("/");
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.LoginPage();
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha2]
		public async Task<IActionResult> LoginClassic(InputModels.LoginInput input) {
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

				return View(viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult AccessDenied() => View();

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Lockout() {
			await AccountRepository.SignOut();
			return View();
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
			return View(new ViewModels.Account.RegisterPage());
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha2]
		public async Task<IActionResult> Register(InputModels.RegisterInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountRepository.Register(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				await AccountRepository.SignOut();

				var viewModel = new ViewModels.Account.RegisterPage {
					DisplayName = input.DisplayName,
					Email = input.Email,
					Password = input.Password,
				};

				return View(viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword() {
			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ForgotPasswordPage();

			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha2]
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

				return View(viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPasswordConfirmation() => View();

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(string code = "") {
			if (string.IsNullOrEmpty(code)) {
				throw new HttpBadRequestError();
			}

			await AccountRepository.SignOut();

			var viewModel = new ViewModels.Account.ResetPasswordPage {
				Code = code
			};

			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		[ValidateRecaptcha2]
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

				return View(viewModel);
			}
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPasswordConfirmation() => View();

		[HttpGet]
		public async Task<IActionResult> Delete(string userId = "") {
			if (string.IsNullOrEmpty(userId)) {
				throw new HttpBadRequestError();
			}

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

			return View("Delete", userId);
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmDelete(string userId = "") {
			if (string.IsNullOrEmpty(userId)) {
				throw new HttpBadRequestError();
			}

			if (UserContext.ApplicationUser.Id != userId && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			var records = await AccountRepository.Records();
			var deletedAccount = records.FirstOrDefault(item => item.DisplayName == "Deleted Account");

			if (deletedAccount is null) {
				throw new HttpNotFoundError();
			}

			await AccountRepository.MergeAccounts(userId, deletedAccount.Id, true);

			return Redirect("/");
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Merge(string userId = "") {
			if (string.IsNullOrEmpty(userId)) {
				throw new HttpBadRequestError();
			}

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

			return View(viewModel);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> ConfirmMerge(string sourceId = "", string targetId = "") {
			if (string.IsNullOrEmpty(sourceId)) {
				throw new HttpBadRequestError();
			}

			if (string.IsNullOrEmpty(targetId)) {
				throw new HttpBadRequestError();
			}

			await AccountRepository.MergeAccounts(sourceId, targetId, false);

			return RedirectToAction(nameof(Account.Details), nameof(Account), new { id = targetId });
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