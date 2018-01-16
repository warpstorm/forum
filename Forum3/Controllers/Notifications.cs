using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	public class Notifications : ForumController {
		NotificationService NotificationService { get; }

		public Notifications(
			NotificationService notificationService
		) {
			NotificationService = notificationService;
		}

		[HttpGet]
		public IActionResult Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read"))
				showRead = true;

			var viewModel = NotificationService.IndexPage(showRead);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Open(int id) {
			var serviceResponse = await NotificationService.Open(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public ActionResult MarkAllRead() {
			return RedirectToAction(nameof(Boards.Index), nameof(Boards));
		}
	}
}