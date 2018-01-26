using Forum3.Annotations;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;

	[Authorize(Roles="Admin")]
	public class SiteSettings : ForumController {
		SiteSettingsService SiteSettingsService { get; }

		public SiteSettings(
			SiteSettingsService siteSettingsService
		) {
			SiteSettingsService = siteSettingsService;
		}

		[HttpGet]
		public IActionResult Index() {
			var viewModel = SiteSettingsService.IndexPage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public IActionResult Edit(InputModels.EditSettingsInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = SiteSettingsService.Edit(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}
	}
}