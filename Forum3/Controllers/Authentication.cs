using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Forum3.Interfaces.Users;
using Forum3.Models.DataModels;
using Forum3.Models.InputModels;
using Forum3.Models.ViewModels.Authentication;
using Forum3.Services;
using Forum3.Helpers;

namespace Forum3.Controllers {
	[AllowAnonymous]
	public class Authentication : ForumController {
		UserManager<ApplicationUser> UserManager { get; }
		SignInManager<ApplicationUser> SignInManager { get; }
		IEmailSender EmailSender { get; }
		ISmsSender SmsSender { get; }
		ILogger Logger { get; }
		string ExternalCookieScheme { get; }

		public Authentication(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IEmailSender emailSender,
			ISmsSender smsSender,
			ILoggerFactory loggerFactory,
			IOptions<IdentityCookieOptions> identityCookieOptions,
			UserService userService
		) : base(userService) {
			UserManager = userManager;
			SignInManager = signInManager;
			EmailSender = emailSender;
			SmsSender = smsSender;
			Logger = loggerFactory.CreateLogger<Authentication>();
			ExternalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
		}

		[HttpGet]
		public async Task<IActionResult> Login(string returnUrl = null) {
			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.Authentication.SignOutAsync(ExternalCookieScheme);

			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null) {
			ViewData["ReturnUrl"] = returnUrl;

			if (ModelState.IsValid) {
				// This doesn't count login failures towards account lockout
				// To enable password failures to trigger account lockout, set lockoutOnFailure: true
				var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

				if (result.Succeeded) {
					Logger.LogInformation(1, "User logged in.");
					return RedirectToLocal(returnUrl);
				}

				if (result.RequiresTwoFactor) {
					return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
				}

				if (result.IsLockedOut) {
					Logger.LogWarning(2, "User account locked out.");
					return View("Lockout");
				}
				else {
					ModelState.AddModelError(string.Empty, "Invalid login attempt.");
					return View(model);
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		[HttpGet]
		public IActionResult Register(string returnUrl = null) {
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterInputModel input, string returnUrl = null) {
			ViewData["ReturnUrl"] = returnUrl;

			if (ModelState.IsValid) {
				var user = new ApplicationUser {
					DisplayName = input.DisplayName,
					UserName = input.Email,
					Email = input.Email
				};

				var result = await UserManager.CreateAsync(user, input.Password);

				if (result.Succeeded) {
					var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);

					var callbackUrl = Url.Action(nameof(ConfirmEmail), nameof(Authentication), new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

					Logger.LogInformation(3, "User created a new account with password.");

					// Automatically redirects on loopback addresses. Useful for development environments that don't have email.
					if (Request.IsLocal() || EmailSender == null)
						return Redirect(callbackUrl);

					await EmailSender.SendEmailAsync(input.Email, "Confirm your account", $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
					return RedirectToLocal(returnUrl);
				}

				AddErrors(result);
			}

			return View(new RegisterViewModel {
				Email = input.Email,
				DisplayName = input.DisplayName
			});
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout() {
			await SignInManager.SignOutAsync();
			Logger.LogInformation(4, "User logged out.");

			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ExternalLogin(string provider, string returnUrl = null) {
			// Request a redirect to the external login provider.
			var redirectUrl = Url.Action(nameof(Authentication.ExternalLoginCallback), nameof(Authentication), new { ReturnUrl = returnUrl });
			var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

			return Challenge(properties, provider);
		}

		[HttpGet]
		public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null) {
			if (remoteError != null) {
				ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
				return View(nameof(Login));
			}

			var info = await SignInManager.GetExternalLoginInfoAsync();

			if (info == null)
				return RedirectToAction(nameof(Login));

			// Sign in the user with this external login provider if the user already has a login.
			var result = await SignInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);

			if (result.Succeeded) {
				Logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
				return RedirectToLocal(returnUrl);
			}

			if (result.RequiresTwoFactor) {
				return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
			}

			if (result.IsLockedOut) {
				return View("Lockout");
			}
			else {
				// If the user does not have an account, then ask the user to create an account.
				ViewData["ReturnUrl"] = returnUrl;
				ViewData["LoginProvider"] = info.LoginProvider;

				var email = info.Principal.FindFirstValue(ClaimTypes.Email);

				return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null) {
			if (ModelState.IsValid) {
				// Get the information about the user from the external login provider
				var info = await SignInManager.GetExternalLoginInfoAsync();

				if (info == null)
					return View("ExternalLoginFailure");

				var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
				var result = await UserManager.CreateAsync(user);

				if (result.Succeeded) {
					result = await UserManager.AddLoginAsync(user, info);

					if (result.Succeeded) {
						await SignInManager.SignInAsync(user, isPersistent: false);
						Logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
						return RedirectToLocal(returnUrl);
					}
				}

				AddErrors(result);
			}

			ViewData["ReturnUrl"] = returnUrl;

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmEmail(string userId, string code) {
			if (userId == null || code == null)
				return View("Error");

			var user = await UserManager.FindByIdAsync(userId);

			if (user == null)
				return View("Error");

			var result = await UserManager.ConfirmEmailAsync(user, code);

			return View(result.Succeeded ? "ConfirmEmail" : "Error");
		}

		[HttpGet]
		public IActionResult ForgotPassword() {
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model) {
			if (ModelState.IsValid) {
				var user = await UserManager.FindByNameAsync(model.Email);

				if (user == null || !(await UserManager.IsEmailConfirmedAsync(user))) {
					// Don't reveal that the user does not exist or is not confirmed
					return View("ForgotPasswordConfirmation");
				}

				// Send an email with this link
				var code = await UserManager.GeneratePasswordResetTokenAsync(user);

				var callbackUrl = Url.Action(nameof(ResetPassword), nameof(Authentication), new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

				//Automatically redirects on loopback addresses. Useful for development environments that don't have email.
				if (Request.IsLocal() || EmailSender == null)
					return Redirect(callbackUrl);

				await EmailSender.SendEmailAsync(model.Email, "Reset Password", $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
				return View("ForgotPasswordConfirmation");
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		[HttpGet]
		public IActionResult ForgotPasswordConfirmation() {
			return View();
		}

		[HttpGet]
		public IActionResult ResetPassword(string code = null) {
			return code == null ? View("Error") : View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
			if (!ModelState.IsValid)
				return View(model);

			var user = await UserManager.FindByNameAsync(model.Email);

			if (user == null)
				// Don't reveal that the user does not exist
				return RedirectToAction(nameof(Authentication.ResetPasswordConfirmation), nameof(Authentication));

			var result = await UserManager.ResetPasswordAsync(user, model.Code, model.Password);

			if (result.Succeeded)
				return RedirectToAction(nameof(Authentication.ResetPasswordConfirmation), nameof(Authentication));

			AddErrors(result);

			return View();
		}

		[HttpGet]
		public IActionResult ResetPasswordConfirmation() {
			return View();
		}

		[HttpGet]
		public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false) {
			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();

			if (user == null)
				return View("Error");

			var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(user);
			var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();

			return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendCode(SendCodeViewModel model) {
			if (!ModelState.IsValid)
				return View();

			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();

			if (user == null)
				return View("Error");

			// Generate the token and send it
			var code = await UserManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);

			if (string.IsNullOrWhiteSpace(code))
				return View("Error");

			var message = "Your security code is: " + code;

			if (model.SelectedProvider == "Email")
				await EmailSender.SendEmailAsync(await UserManager.GetEmailAsync(user), "Security Code", message);
			else if (model.SelectedProvider == "Phone")
				await SmsSender.SendSmsAsync(await UserManager.GetPhoneNumberAsync(user), message);

			return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
		}

		[HttpGet]
		public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null) {
			// Require that the user has already logged in via username/password or external login
			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();

			if (user == null)
				return View("Error");

			return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model) {
			if (!ModelState.IsValid)
				return View(model);

			// The following code protects for brute force attacks against the two factor codes.
			// If a user enters incorrect codes for a specified amount of time then the user account
			// will be locked out for a specified amount of time.
			var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);

			if (result.Succeeded)
				return RedirectToLocal(model.ReturnUrl);

			if (result.IsLockedOut) {
				Logger.LogWarning(7, "User account locked out.");
				return View("Lockout");
			}
			else {
				ModelState.AddModelError(string.Empty, "Invalid code.");
				return View(model);
			}
		}

		void AddErrors(IdentityResult result) {
			foreach (var error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);
		}

		IActionResult RedirectToLocal(string returnUrl) {
			if (Url.IsLocalUrl(returnUrl))
				return Redirect(returnUrl);
			else
				return RedirectToAction(nameof(Topics.Index), nameof(Topics));
		}
	}
}
