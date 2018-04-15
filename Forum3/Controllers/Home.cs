using Forum3.Contexts;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Forum3.Controllers {
	using ViewModels = Models.ViewModels;

	public class Home : ForumController {
		UserContext UserContext { get; }
		SettingsRepository SettingsRepository { get; }

		public Home(
			UserContext userContext,
			SettingsRepository settingsRepository
		) {
			UserContext = userContext;
			SettingsRepository = settingsRepository;
		}

		[HttpGet]
		public IActionResult FrontPage() {
			var frontpage = SettingsRepository.FrontPage();

			switch (frontpage) {
				default:
				case "Board List":
					return RedirectToAction(nameof(Boards.Index), nameof(Boards));

				case "All Topics":
					return RedirectToAction(nameof(Topics.Index), nameof(Topics), new { id = 0 });

				case "Unread Topics":
					return RedirectToAction(nameof(Topics.Index), nameof(Topics), new { id = 0, unread = 1 });
			}
		}

		[AllowAnonymous]
		public IActionResult Error() {
			var viewModel = new ViewModels.Error {
				RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
			};

			return View(viewModel);
		}
	}
}