using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services.Controller;

namespace Forum3.Controllers {
	public class Notifications : ForumController {
		NotificationService NotificationService { get; }

		public Notifications(
			NotificationService notificationService
		) {
			NotificationService = notificationService;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read"))
				showRead = true;

			var viewModel = await NotificationService.IndexPage(showRead);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Open(int id) {
			var serviceResponse = await NotificationService.Open(id);

			if (string.IsNullOrEmpty(serviceResponse.RedirectPath))
				return RedirectToReferrer();
			else
				return Redirect(serviceResponse.RedirectPath);
		}

		[HttpGet]
		public ActionResult MarkAllRead() {
			return RedirectToAction(nameof(Notifications.Index), nameof(Notifications));
		}
	}
}