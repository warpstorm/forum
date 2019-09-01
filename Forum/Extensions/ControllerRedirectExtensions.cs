using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Forum.Extensions {
	using ServiceModels = Models.ServiceModels;

	public static class ControllerRedirectExtensions {
		public static IActionResult RedirectToReferrer(this Controller controller) {
			var referrer = controller.GetReferrer();
			return controller.Redirect(referrer);
		}

		public static async Task<IActionResult> RedirectFromService(this Controller controller, ServiceModels.ServiceResponse serviceResponse = null, Func<Task<IActionResult>> failAsync = null, Func<IActionResult> failSync = null) {
			if (!(serviceResponse is null)) {
				if (!string.IsNullOrEmpty(serviceResponse.Message)) {
					controller.TempData[Constants.InternalKeys.StatusMessage] = serviceResponse.Message;
				}

				foreach (var kvp in serviceResponse.Errors) {
					controller.ModelState.AddModelError(kvp.Key, kvp.Value);
				}

				if (serviceResponse.Success) {
					var redirectPath = serviceResponse.RedirectPath;

					if (string.IsNullOrEmpty(redirectPath)) {
						redirectPath = controller.GetReferrer();
					}

					return controller.Redirect(redirectPath);
				}
			}

			if (!(failAsync is null)) {
				return await failAsync();
			}
			else if (!(failSync is null)) {
				return failSync();
			}
			else {
				var redirectPath = controller.GetReferrer();
				return controller.Redirect(redirectPath);
			}
		}

		public static string GetReferrer(this Controller controller) {
			controller.Request.Query.TryGetValue("ReturnUrl", out var referrer);

			if (string.IsNullOrEmpty(referrer)) {
				controller.Request.Query.TryGetValue("Referer", out referrer);
			}

			if (string.IsNullOrEmpty(referrer)) {
				referrer = controller.Request.Headers["Referer"].ToString();
			}

			if (string.IsNullOrEmpty(referrer)) {
				referrer = "/";
			}

			return referrer;
		}
	}
}