using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize]
	[RequireRemoteHttps]
	public class Topics : ForumController {
		public TopicService TopicService { get; }
		public MessageService MessageService { get; }

		public Topics(TopicService topicService, MessageService messageService) {
			TopicService = topicService;
			MessageService = messageService;
		}

		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Index(int page = 1) {
			var viewModel = await TopicService.IndexPage(page);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Display(int id, int page = 1, int target = 0) {
			var viewModel = await TopicService.DisplayPage(id, page);

			if (string.IsNullOrEmpty(viewModel.RedirectPath))
				return View(viewModel);
			else
				return Redirect(viewModel.RedirectPath);
		}
	}
}