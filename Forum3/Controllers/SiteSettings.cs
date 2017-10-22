using Forum3.Annotations;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;

	public class SiteSettings : ForumController {
		SiteSettingsService SiteSettingsService { get; }

		public SiteSettings(
			SiteSettingsService siteSettingsService
		) {
			SiteSettingsService = siteSettingsService;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = await SiteSettingsService.IndexPage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(InputModels.EditSettingsInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await SiteSettingsService.Edit(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectToReferrer();
		}
	}
}