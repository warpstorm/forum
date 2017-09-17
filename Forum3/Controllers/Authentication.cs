using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Forum3.Models.ViewModels.Authentication;
using Forum3.Models.DataModels;
using Forum3.Interfaces.Users;
using Forum3.Helpers;

namespace Forum3.Controllers {
	/// <summary>
	/// Most of this is OOB MVC authentication code.
	/// Eventually this will be migrated and cleaned up under an AuthenticationService class.
	/// </summary>
	[AllowAnonymous]
	public class Authentication : ForumController {
		UserManager<ApplicationUser> UserManager { get; }
		SignInManager<ApplicationUser> SignInManager { get; }
		IEmailSender EmailSender { get; }
		ILogger Logger { get; }

		public Authentication(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IEmailSender emailSender,
			ILogger<Authentication> logger
		) {
			UserManager = userManager;
			SignInManager = signInManager;
			EmailSender = emailSender;
			Logger = logger;
		}

		[TempData]
		public string ErrorMessage { get; set; }

		[HttpGet]
		public async Task<IActionResult> Login(string returnUrl = null) {
			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

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
					Logger.LogInformation("User logged in.");
					return RedirectToLocal(returnUrl);
				}
				if (result.RequiresTwoFactor) {
					return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
				}
				if (result.IsLockedOut) {
					Logger.LogWarning("User account locked out.");
					return RedirectToAction(nameof(Lockout));
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
		public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null) {
			// Ensure the user has gone through the username & password screen first
			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();

			if (user == null) {
				throw new ApplicationException($"Unable to load two-factor authentication user.");
			}

			var model = new LoginWith2faViewModel { RememberMe = rememberMe };
			ViewData["ReturnUrl"] = returnUrl;

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null) {
			if (!ModelState.IsValid) {
				return View(model);
			}

			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();
			if (user == null) {
				throw new ApplicationException($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
			}

			var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

			var result = await SignInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

			if (result.Succeeded) {
				Logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
				return RedirectToLocal(returnUrl);
			}
			else if (result.IsLockedOut) {
				Logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
				return RedirectToAction(nameof(Lockout));
			}
			else {
				Logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
				ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
				return View();
			}
		}

		[HttpGet]
		public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null) {
			// Ensure the user has gone through the username & password screen first
			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();
			if (user == null) {
				throw new ApplicationException($"Unable to load two-factor authentication user.");
			}

			ViewData["ReturnUrl"] = returnUrl;

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null) {
			if (!ModelState.IsValid) {
				return View(model);
			}

			var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();

			if (user == null) {
				throw new ApplicationException($"Unable to load two-factor authentication user.");
			}

			var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

			var result = await SignInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

			if (result.Succeeded) {
				Logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
				return RedirectToLocal(returnUrl);
			}
			if (result.IsLockedOut) {
				Logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
				return RedirectToAction(nameof(Lockout));
			}
			else {
				Logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
				ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
				return View();
			}
		}

		[HttpGet]
		public IActionResult Lockout() {
			return View();
		}

		[HttpGet]
		public IActionResult Register(string returnUrl = null) {
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterViewModel input, string returnUrl = null) {
			ViewData["ReturnUrl"] = returnUrl;
			if (ModelState.IsValid) {
				var user = new ApplicationUser {
					DisplayName = input.DisplayName,
					UserName = input.Email,
					Email = input.Email
				};

				var result = await UserManager.CreateAsync(user, input.Password);
				if (result.Succeeded) {
					Logger.LogInformation("User created a new account with password.");

					var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
					var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

					if (Request.IsLocal() || EmailSender == null)
						return Redirect(callbackUrl);

					await EmailSender.SendEmailConfirmationAsync(input.Email, callbackUrl);

					await SignInManager.SignInAsync(user, isPersistent: false);
					Logger.LogInformation("User created a new account with password.");
					return RedirectToLocal(returnUrl);
				}
				AddErrors(result);
			}

			// If we got this far, something failed, redisplay form
			return View(input);
		}

		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout() {
			await SignInManager.SignOutAsync();
			Logger.LogInformation("User logged out.");
			return RedirectToAction("Index", "Home");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ExternalLogin(string provider, string returnUrl = null) {
			// Request a redirect to the external login provider.
			var redirectUrl = Url.Action(nameof(ExternalLoginCallback), nameof(Authentication), new { returnUrl });
			var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			return Challenge(properties, provider);
		}

		[HttpGet]
		public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null) {
			if (remoteError != null) {
				ErrorMessage = $"Error from external provider: {remoteError}";
				return RedirectToAction(nameof(Login));
			}
			var info = await SignInManager.GetExternalLoginInfoAsync();
			if (info == null) {
				return RedirectToAction(nameof(Login));
			}

			// Sign in the user with this external login provider if the user already has a login.
			var result = await SignInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
			if (result.Succeeded) {
				Logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
				return RedirectToLocal(returnUrl);
			}
			if (result.IsLockedOut) {
				return RedirectToAction(nameof(Lockout));
			}
			else {
				// If the user does not have an account, then ask the user to create an account.
				ViewData["ReturnUrl"] = returnUrl;
				ViewData["LoginProvider"] = info.LoginProvider;
				var email = info.Principal.FindFirstValue(ClaimTypes.Email);
				return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null) {
			if (ModelState.IsValid) {
				// Get the information about the user from the external login provider
				var info = await SignInManager.GetExternalLoginInfoAsync();
				if (info == null) {
					throw new ApplicationException("Error loading external login information during confirmation.");
				}
				var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
				var result = await UserManager.CreateAsync(user);
				if (result.Succeeded) {
					result = await UserManager.AddLoginAsync(user, info);
					if (result.Succeeded) {
						await SignInManager.SignInAsync(user, isPersistent: false);
						Logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
						return RedirectToLocal(returnUrl);
					}
				}
				AddErrors(result);
			}

			ViewData["ReturnUrl"] = returnUrl;
			return View(nameof(ExternalLogin), model);
		}

		[HttpGet]
		public async Task<IActionResult> ConfirmEmail(string userId, string code) {
			if (userId == null || code == null) {
				return RedirectToAction("Index", "Home");
			}
			var user = await UserManager.FindByIdAsync(userId);
			if (user == null) {
				throw new ApplicationException($"Unable to load user with ID '{userId}'.");
			}
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
				var user = await UserManager.FindByEmailAsync(model.Email);
				if (user == null || !(await UserManager.IsEmailConfirmedAsync(user))) {
					// Don't reveal that the user does not exist or is not confirmed
					return RedirectToAction(nameof(ForgotPasswordConfirmation));
				}

				// For more information on how to enable account confirmation and password reset please
				// visit https://go.microsoft.com/fwlink/?LinkID=532713
				var code = await UserManager.GeneratePasswordResetTokenAsync(user);
				var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
				await EmailSender.SendEmailAsync(model.Email, "Reset Password",
				   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
				return RedirectToAction(nameof(ForgotPasswordConfirmation));
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
			if (code == null) {
				throw new ApplicationException("A code must be supplied for password reset.");
			}
			var model = new ResetPasswordViewModel { Code = code };
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
			if (!ModelState.IsValid) {
				return View(model);
			}
			var user = await UserManager.FindByEmailAsync(model.Email);
			if (user == null) {
				// Don't reveal that the user does not exist
				return RedirectToAction(nameof(ResetPasswordConfirmation));
			}
			var result = await UserManager.ResetPasswordAsync(user, model.Code, model.Password);
			if (result.Succeeded) {
				return RedirectToAction(nameof(ResetPasswordConfirmation));
			}
			AddErrors(result);
			return View();
		}

		[HttpGet]
		public IActionResult ResetPasswordConfirmation() {
			return View();
		}

		[HttpGet]
		[Authorize]
		public IActionResult AccessDenied() {
			return View();
		}
	}
}
