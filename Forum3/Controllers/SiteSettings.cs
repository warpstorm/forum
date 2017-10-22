using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	public class SiteSettings : ForumController {
		SiteSettingsService SiteSettingsService { get; }

		public SiteSettings(
			SiteSettingsService siteSettingsService
		) {
			siteSettingsService = SiteSettingsService;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = await SiteSettingsService.IndexPage();
			return View(viewModel);
		}
	}
}