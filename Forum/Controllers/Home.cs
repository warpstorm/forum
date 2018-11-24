using Forum.Contexts;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels;

	public class Home : Controller {
		SettingsRepository SettingsRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Home(
			IForumViewResult forumViewResult,
			SettingsRepository settingsRepository
		) {
			ForumViewResult = forumViewResult;
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

			return ForumViewResult.ViewResult(this, viewModel);
		}
	}
}