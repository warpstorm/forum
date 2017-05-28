using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Forum3.Enums;
using Forum3.Models.ViewModels.Profile;
using Forum3.Models.ViewModels.Profile.Pages;
using DataModels = Forum3.Models.DataModels;
using InputModels = Forum3.Models.InputModels;

namespace Forum3.Controllers {
	[Authorize]
	public class Profile : ForumController {
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		ILogger Logger { get; }
		string ExternalCookieScheme { get; }

		public Profile(
			UserManager<DataModels.ApplicationUser> userManager,
			SignInManager<DataModels.ApplicationUser> signInManager,
			ILoggerFactory loggerFactory,
			IOptions<IdentityCookieOptions> identityCookieOptions
		) {
			UserManager = userManager;
			SignInManager = signInManager;
			Logger = loggerFactory.CreateLogger<Profile>();
			ExternalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
		}

		[HttpGet]
		public async Task<IActionResult> Manage(EManageMessageId? message = null) {
			ViewData["StatusMessage"] =
				message == EManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == EManageMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == EManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
				: message == EManageMessageId.Error ? "An error has occurred."
				: message == EManageMessageId.AddPhoneSuccess ? "Your phone number was added."
				: message == EManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
				: "";

			var user = await GetCurrentUserAsync();

			if (user == null)
				return View("Error");

			var model = new ManagePageViewModel {
				DisplayName = user.DisplayName,
				HasPassword = await UserManager.HasPasswordAsync(user),
				Logins = await UserManager.GetLoginsAsync(user),
				BrowserRemembered = await SignInManager.IsTwoFactorClientRememberedAsync(user)
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Manage(InputModels.ProfileInput input) {
			// TODO - save changes

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account) {
			EManageMessageId? message = EManageMessageId.Error;
			var user = await GetCurrentUserAsync();
			if (user != null) {
				var result = await UserManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
				if (result.Succeeded) {
					await SignInManager.SignInAsync(user, isPersistent: false);
					message = EManageMessageId.RemoveLoginSuccess;
				}
			}
			return RedirectToAction(nameof(ManageLogins), new { Message = message });
		}

		[HttpGet]
		public IActionResult ChangePassword() {
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) {
			if (!ModelState.IsValid) {
				return View(model);
			}
			var user = await GetCurrentUserAsync();
			if (user != null) {
				var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
				if (result.Succeeded) {
					await SignInManager.SignInAsync(user, isPersistent: false);
					Logger.LogInformation(3, "User changed their password successfully.");
					return RedirectToAction(nameof(Manage), new { Message = EManageMessageId.ChangePasswordSuccess });
				}
				AddErrors(result);
				return View(model);
			}
			return RedirectToAction(nameof(Manage), new { Message = EManageMessageId.Error });
		}

		[HttpGet]
		public IActionResult SetPassword() {
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SetPassword(SetPasswordViewModel model) {
			if (!ModelState.IsValid) {
				return View(model);
			}

			var user = await GetCurrentUserAsync();
			if (user != null) {
				var result = await UserManager.AddPasswordAsync(user, model.NewPassword);
				if (result.Succeeded) {
					await SignInManager.SignInAsync(user, isPersistent: false);
					return RedirectToAction(nameof(Manage), new { Message = EManageMessageId.SetPasswordSuccess });
				}
				AddErrors(result);
				return View(model);
			}
			return RedirectToAction(nameof(Manage), new { Message = EManageMessageId.Error });
		}

		[HttpGet]
		public async Task<IActionResult> ManageLogins(EManageMessageId? message = null) {
			ViewData["StatusMessage"] =
				message == EManageMessageId.RemoveLoginSuccess ? "The external login was removed."
				: message == EManageMessageId.AddLoginSuccess ? "The external login was added."
				: message == EManageMessageId.Error ? "An error has occurred."
				: "";
			var user = await GetCurrentUserAsync();
			if (user == null) {
				return View("Error");
			}
			var userLogins = await UserManager.GetLoginsAsync(user);
			var otherLogins = SignInManager.GetExternalAuthenticationSchemes().Where(auth => userLogins.All(ul => auth.AuthenticationScheme != ul.LoginProvider)).ToList();
			ViewData["ShowRemoveButton"] = user.PasswordHash != null || userLogins.Count > 1;
			return View(new ManageLoginsViewModel {
				CurrentLogins = userLogins,
				OtherLogins = otherLogins
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LinkLogin(string provider) {
			// Clear the existing external cookie to ensure a clean login process
			await HttpContext.Authentication.SignOutAsync(ExternalCookieScheme);

			// Request a redirect to the external login provider to link a login for the current user
			var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
			var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, UserManager.GetUserId(User));
			return Challenge(properties, provider);
		}

		[HttpGet]
		public async Task<ActionResult> LinkLoginCallback() {
			var user = await GetCurrentUserAsync();

			if (user == null)
				return View("Error");

			var info = await SignInManager.GetExternalLoginInfoAsync(await UserManager.GetUserIdAsync(user));

			if (info == null)
				return RedirectToAction(nameof(ManageLogins), new { Message = EManageMessageId.Error });

			var result = await UserManager.AddLoginAsync(user, info);
			var message = result.Succeeded ? EManageMessageId.AddLoginSuccess : EManageMessageId.Error;

			if (result.Succeeded) {
				message = EManageMessageId.AddLoginSuccess;

				// Clear the existing external cookie to ensure a clean login process
				await HttpContext.Authentication.SignOutAsync(ExternalCookieScheme);
			}

			return RedirectToAction(nameof(ManageLogins), new { Message = message });
		}

		void AddErrors(IdentityResult result) {
			foreach (var error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);
		}

		Task<DataModels.ApplicationUser> GetCurrentUserAsync() {
			return UserManager.GetUserAsync(HttpContext.User);
		}
	}
}
