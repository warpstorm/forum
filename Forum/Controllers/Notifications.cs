using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Mvc;

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
		public IActionResult Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read")) {
				showRead = true;
			}

			var notifications = NotificationRepository.Index(showRead);

			var viewModel = new ViewModels.Pages.IndexPage {
				Notifications = notifications
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult Open(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = NotificationRepository.Open(id);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return ForumViewResult.RedirectToReferrer(this);
			}
		}

		[HttpGet]
		public ActionResult MarkAllRead() => Redirect("/");
	}
}