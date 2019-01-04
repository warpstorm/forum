using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels.Notifications;

	public class Notifications : Controller {
		NotificationRepository NotificationRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Notifications(
			NotificationRepository notificationRepository,
			IForumViewResult forumViewResult
		) {
			NotificationRepository = notificationRepository;
			ForumViewResult = forumViewResult;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read")) {
				showRead = true;
			}

			var notifications = await NotificationRepository.Index(showRead);

			var viewModel = new ViewModels.Pages.IndexPage {
				Notifications = notifications
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Open(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = NotificationRepository.Open(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public ActionResult MarkAllRead() => Redirect("/");
	}
}