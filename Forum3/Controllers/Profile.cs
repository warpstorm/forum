using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Forum3.Models.DataModels;
using Forum3.Models.ViewModels.Profile;
using Forum3.Interfaces.Users;
using Forum3.Enums;
using Forum3.Models.ViewModels.Profile.Pages;
using Forum3.Services;
using InputModels = Forum3.Models.InputModels;

namespace Forum3.Controllers {
	[Authorize]
	public class Profile : ForumController {
		UserManager<ApplicationUser> UserManager { get; }
		SignInManager<ApplicationUser> SignInManager { get; }
		IEmailSender EmailSender { get; }
		ILogger Logger { get; }

		public Profile(
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IEmailSender emailSender,
			ILoggerFactory loggerFactory,
			UserService userService
		) : base(userService) {
			UserManager = userManager;
			SignInManager = signInManager;
			EmailSender = emailSender;
			Logger = loggerFactory.CreateLogger<Profile>();
		}

		[HttpGet]
		public async Task<IActionResult> Manage(ManageMessageId? message = null) {
			ViewData["StatusMessage"] =
				message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
				: message == ManageMessageId.Error ? "An error has occurred."
				: message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
				: message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
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
		public async Task<IActionResult> Manage(InputModels.ProfileInput input) {
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel account) {
			ManageMessageId? message = ManageMessageId.Error;
			var user = await GetCurrentUserAsync();
			if (user != null) {
				var result = await UserManager.RemoveLoginAsync(user, account.LoginProvider, account.ProviderKey);
				if (result.Succeeded) {
					await SignInManager.SignInAsync(user, isPersistent: false);
					message = ManageMessageId.RemoveLoginSuccess;
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
					return RedirectToAction(nameof(Manage), new { Message = ManageMessageId.ChangePasswordSuccess });
				}
				AddErrors(result);
				return View(model);
			}
			return RedirectToAction(nameof(Manage), new { Message = ManageMessageId.Error });
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
					return RedirectToAction(nameof(Manage), new { Message = ManageMessageId.SetPasswordSuccess });
				}
				AddErrors(result);
				return View(model);
			}
			return RedirectToAction(nameof(Manage), new { Message = ManageMessageId.Error });
		}

		[HttpGet]
		public async Task<IActionResult> ManageLogins(ManageMessageId? message = null) {
			ViewData["StatusMessage"] =
				message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
				: message == ManageMessageId.AddLoginSuccess ? "The external login was added."
				: message == ManageMessageId.Error ? "An error has occurred."
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
		public IActionResult LinkLogin(string provider) {
			// Request a redirect to the external login provider to link a login for the current user
			var redirectUrl = Url.Action("LinkLoginCallback", "Manage");
			var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, UserManager.GetUserId(User));
			return Challenge(properties, provider);
		}

		[HttpGet]
		public async Task<ActionResult> LinkLoginCallback() {
			var user = await GetCurrentUserAsync();
			if (user == null) {
				return View("Error");
			}
			var info = await SignInManager.GetExternalLoginInfoAsync(await UserManager.GetUserIdAsync(user));
			if (info == null) {
				return RedirectToAction(nameof(ManageLogins), new { Message = ManageMessageId.Error });
			}
			var result = await UserManager.AddLoginAsync(user, info);
			var message = result.Succeeded ? ManageMessageId.AddLoginSuccess : ManageMessageId.Error;
			return RedirectToAction(nameof(ManageLogins), new { Message = message });
		}

		void AddErrors(IdentityResult result) {
			foreach (var error in result.Errors)
				ModelState.AddModelError(string.Empty, error.Description);
		}

		Task<ApplicationUser> GetCurrentUserAsync() {
			return UserManager.GetUserAsync(HttpContext.User);
		}
	}
}
