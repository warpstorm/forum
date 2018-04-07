using Forum3.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using ViewModels = Models.ViewModels.Notifications;

	public class Notifications : ForumController {
		NotificationRepository NotificationRepository { get; }

		public Notifications(
			NotificationRepository notificationRepository
		) {
			NotificationRepository = notificationRepository;
		}

		[HttpGet]
		public IActionResult Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read"))
				showRead = true;

			var notifications = NotificationRepository.Index(showRead);

			var viewModel = new ViewModels.Pages.IndexPage {
				Notifications = notifications
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Open(int id) {
			var serviceResponse = await NotificationRepository.Open(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public ActionResult MarkAllRead() {
			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}
	}
}