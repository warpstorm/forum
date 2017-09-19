using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;

using InputModels = Forum3.Models.InputModels;

namespace Forum3.Controllers {
	public class Account : ForumController {
		AccountService AccountService { get; }

		public Account(
			AccountService accountService
		) {
			AccountService = accountService;
		}

		[HttpGet]
		public async Task<IActionResult> Details(string id) {
			var viewModel = await AccountService.DetailsPage(id);
			ModelState.Clear();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Details(InputModels.UpdateAccountInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.UpdateAccount(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			var viewModel = await AccountService.DetailsPage(input);
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateAvatar(InputModels.UpdateAvatarInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.UpdateAvatar(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			var viewModel = await AccountService.DetailsPage(input.DisplayName);
			return View(nameof(Details), viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendVerificationEmail() {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.SendVerificationEmail();
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			return RedirectToAction(nameof(Profile.Details), nameof(Profile));
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmEmail(InputModels.ConfirmEmailInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.ConfirmEmail(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Boards.Index), nameof(Boards));
				}
			}

			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Login() {
			var viewModel = await AccountService.LoginPage();
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(InputModels.LoginInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.Login(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToReferrer();
				}
			}

			var viewModel = await AccountService.LoginPage(input);
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult AccessDenied() {
			return View();
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Lockout() {
			await AccountService.SignOut();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout() {
			await AccountService.SignOut();
			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Register() {
			var viewModel = await AccountService.RegisterPage();
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(InputModels.RegisterInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.Register(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Login));
				}
			}

			var viewModel = await AccountService.RegisterPage(input);
			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword() {
			var viewModel = await AccountService.ForgotPasswordPage();
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(InputModels.ForgotPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.ForgotPassword(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Login));
				}
			}

			var viewModel = await AccountService.ForgotPasswordPage(input);
			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPasswordConfirmation() => View();

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(string code) {
			var viewModel = await AccountService.ResetPasswordPage(code);
			return View(viewModel);
		}

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(InputModels.ResetPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.ResetPassword(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);
					else
						return RedirectToAction(nameof(Login));
				}
			}

			var viewModel = await AccountService.ResetPasswordPage(input.Code);
			return View(viewModel);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPasswordConfirmation() => View();
	}
}