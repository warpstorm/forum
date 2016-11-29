using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize]
	public class Topics : Controller {
		public TopicService TopicService { get; }
		public MessageService MessageService { get; }

		public Topics(TopicService topicService, MessageService messageService) {
			TopicService = topicService;
			MessageService = messageService;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Index() {
			var skip = 0;

			if (Request.Query.ContainsKey("skip"))
				skip = Convert.ToInt32(Request.Query["skip"]);

			var take = 15;

			if (Request.Query.ContainsKey("take"))
				take = Convert.ToInt32(Request.Query["take"]);

			var viewModel = await TopicService.GetTopicIndex(skip, take);

			return View(viewModel);
		}

		public async Task<IActionResult> Display(int id, int page = 1) {
			var jumpToLatest = false;

			if (page == 0) {
				page = 1;
				jumpToLatest = true;
			}

			int take = 15;
			int skip = (page * take) - take;

			var message = MessageService.Find(id);

			if (message.ParentId > 0) {
				var actionUrl = Url.Action(nameof(Display), new { id = message.ParentId });
				return new RedirectResult(actionUrl + "#message" + id);
			}

			var viewModel = await TopicService.GetTopic(message, page, skip, take, jumpToLatest);

			return View(viewModel);
		}
	}
}