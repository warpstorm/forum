using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;

using InputModels = Forum3.Models.InputModels;

namespace Forum3.Controllers {
	[AllowAnonymous]
	public class Account : ForumController {
		AccountService AccountService { get; }

		public Account(
			AccountService accountService
		) {
			AccountService = accountService;
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Details(string id) {
			var viewModel = await AccountService.DetailsPage(id);
			return View(viewModel);
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Details(InputModels.UpdateAccountInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.UpdateAccount(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectToReferrer();
			}

			var viewModel = await AccountService.DetailsPage(input);
			return View(viewModel);
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendVerificationEmail() {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.SendVerificationEmail();
				ProcessServiceResponse(serviceResponse);

				if (ModelState.IsValid)
					return RedirectToReferrer();
			}

			return RedirectToAction(nameof(Profile.Details), nameof(Profile));
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> ConfirmEmail(InputModels.ConfirmEmailInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.ConfirmEmail(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectToAction(nameof(Details));
			}

			return View();
		}

		[HttpGet]
		public async Task<IActionResult> Login() {
			var viewModel = await AccountService.LoginPage();
			return View(viewModel);
		}
		
		[HttpPost]
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
		[Authorize]
		public IActionResult AccessDenied() {
			return View();
		}

		[HttpGet]
		public async Task<IActionResult> Lockout() {
			await AccountService.SignOut();
			return View();
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout() {
			await AccountService.SignOut();
			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}

		[HttpGet]
		public async Task<IActionResult> Register() {
			var viewModel = await AccountService.RegisterPage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(InputModels.RegisterInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.Register(input);

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
		public async Task<IActionResult> ForgotPassword() {
			var viewModel = await AccountService.ForgotPasswordPage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(InputModels.ForgotPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.ForgotPassword(input);

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
		public IActionResult ForgotPasswordConfirmation() => View();

		[HttpGet]
		public async Task<IActionResult> ResetPassword(string code) {
			var viewModel = await AccountService.ResetPasswordPage(code);
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(InputModels.ResetPasswordInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await AccountService.ResetPassword(input);

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
		public IActionResult ResetPasswordConfirmation() => View();
	}
}