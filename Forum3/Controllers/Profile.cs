using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;

namespace Forum3.Controllers {
	public class Profile : ForumController {
		ProfileService ProfileService { get; }
		UrlEncoder UrlEncoder { get; }

		public Profile(
			ProfileService profileService,
			UrlEncoder urlEncoder
		) {
			ProfileService = profileService;
			UrlEncoder = urlEncoder;
		}
		
		[HttpGet]
		public async Task<IActionResult> Details(string id) {
			var viewModel = await ProfileService.DetailsPage(id);
			return View(viewModel);
		}
	}
}
