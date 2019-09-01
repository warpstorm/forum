using Forum.Services.Repositories;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum.Services {
	using ServiceModels = Models.ServiceModels;

	public class ForumViewResult {
		BoardRepository BoardRepository { get; }

		public ForumViewResult(
			BoardRepository boardRepository
		) {
			BoardRepository = boardRepository;
		}

		public IActionResult RedirectToReferrer(Controller controller) {
			var referrer = GetReferrer(controller);
			return controller.Redirect(referrer);
		}

		public async Task<IActionResult> RedirectFromService(Controller controller, ServiceModels.ServiceResponse serviceResponse = null, Func<Task<IActionResult>> failAsync = null, Func<IActionResult> failSync = null) {
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
						redirectPath = GetReferrer(controller);
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
				var redirectPath = GetReferrer(controller);
				return controller.Redirect(redirectPath);
			}
		}

		public IActionResult RedirectToLocal(Controller controller, string returnUrl) {
			if (controller.Url.IsLocalUrl(returnUrl)) {
				return controller.Redirect(returnUrl);
			}
			else {
				return controller.Redirect("/");
			}
		}

		public async Task<IActionResult> ViewResult(Controller controller, string viewName, object model = null) {
			if (model is Task) {
				// Help to not shoot myself in the foot.
				throw new Exception($"{nameof(model)} is still a task. Did you forget an await?");
			}

			if (controller.Request.Headers["X-Requested-With"] == "XMLHttpRequest") {
				controller.ViewData[Constants.InternalKeys.Layout] = "_LayoutEmpty";
			}

			var requestUrl = controller.Request.GetEncodedUrl();
			controller.ViewData["Url"] = requestUrl;

			var baseUrlMatch = Regex.Match(requestUrl, @"(https?:\/\/.*?\/)");

			if (baseUrlMatch.Success) {
				var baseUrl = baseUrlMatch.Groups[1].Value;
				controller.ViewData["Image"] = $"{baseUrl}images/logos/planet.png";
				controller.ViewData["BaseUrl"] = baseUrl;
			}

			controller.ViewData["Referrer"] = GetReferrer(controller);
			controller.ViewData["Categories"] = await BoardRepository.CategoryIndex();

			return controller.View(viewName, model);
		}
		public async Task<IActionResult> ViewResult(Controller controller, object model) => await ViewResult(controller, null, model);
		public async Task<IActionResult> ViewResult(Controller controller) => await ViewResult(controller, null, null);

		public string GetReferrer(Controller controller) {
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