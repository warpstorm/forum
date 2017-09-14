using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Models.InputModels;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize]
	public class Topics : ForumController {
		public TopicService TopicService { get; }
		public MessageService MessageService { get; }

		public Topics(
			TopicService topicService,
			MessageService messageService
		) {
			TopicService = topicService;
			MessageService = messageService;
		}

		[HttpGet]
		public async Task<IActionResult> Index(int id = 0, int page = 1) {
			var viewModel = await TopicService.IndexPage(id, page);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Display(int id, int page = 1, int target = 0) {
			var viewModel = await TopicService.DisplayPage(id, page, target);

			if (string.IsNullOrEmpty(viewModel.RedirectPath))
				return View(viewModel);
			else
				return Redirect(viewModel.RedirectPath);
		}

		[HttpGet]
		public IActionResult Latest(int id) {
			return RedirectToAction(nameof(Display), new { id = id, page = 0 });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.CreateReply(input);
				ProcessServiceResponse(serviceResponse);

				if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
					return Redirect(serviceResponse.RedirectPath);
			}

			var viewModel = await TopicService.DisplayPage(input.Id);
			viewModel.ReplyForm.Body = input.Body;

			return View(nameof(Display), viewModel);
		}
	}
}