using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels;

	public class Home : Controller {
		AccountRepository AccountRepository { get; }
		SettingsRepository SettingsRepository { get; }
		IForumViewResult ForumViewResult { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		IAntiforgery Xsrf { get; }

		public Home(
			AccountRepository accountRepository,
			SettingsRepository settingsRepository,
			IForumViewResult forumViewResult,
			IHttpContextAccessor httpContextAccessor,
			IAntiforgery xsrf
		) {
			AccountRepository = accountRepository;
			SettingsRepository = settingsRepository;
			ForumViewResult = forumViewResult;
			HttpContextAccessor = httpContextAccessor;
			Xsrf = xsrf;
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
		public async Task<IActionResult> Error() {
			var viewModel = new ViewModels.Error {
				RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> WhosOnline() {
			var viewModel = AccountRepository.GetOnlineList();

			ViewData[Constants.InternalKeys.Layout] = "_LayoutEmpty";

			return await ForumViewResult.ViewResult(this, "Sidebar/_OnlineUsersList", viewModel);
		}

		[HttpGet]
		public IActionResult Token() => Json(new {
			token = Xsrf.GetAndStoreTokens(HttpContextAccessor.HttpContext).RequestToken
		});
	}
}