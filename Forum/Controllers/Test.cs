using Forum.Data.Contexts;
using Forum.ExternalClients.Imgur;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	public class Test : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		ImgurClient ImgurClient { get; }

		public Test(
			ApplicationDbContext dbContext,
			UserContext userContext,
			ImgurClient imgurClient
		) {
			DbContext = dbContext;
			UserContext = userContext;
			ImgurClient = imgurClient;
		}

		[HttpGet]
		public async Task<IActionResult> Test1() {
			await ImgurClient.RefreshToken();

			return View("Error", new Models.ViewModels.Error());
		}

		[HttpGet]
		public async Task<IActionResult> Test2() {
			var result = await ImgurClient.GetFavorites();

			return View("Error", new Models.ViewModels.Error());
		}
	}
}
