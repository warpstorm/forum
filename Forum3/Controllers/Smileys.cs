using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;

namespace Forum3.Controllers {
	public class Smileys : ForumController {
		SmileyService SmileyService { get; }

		public Smileys(
			SmileyService smileyService
		) {
			SmileyService = smileyService;
		}

		public async Task<IActionResult> Index() {
			var viewModel = await SmileyService.IndexPage();
			return View(viewModel);
		}
	}
}