using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Models.InputModels;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize]
	public class Topics : ForumController {
		TopicService TopicService { get; }
		MessageService MessageService { get; }
		SmileyService SmileyService { get; }

		public Topics(
			TopicService topicService,
			MessageService messageService,
			SmileyService smileyService
		) {
			TopicService = topicService;
			MessageService = messageService;
			SmileyService = smileyService;
		}

		[HttpGet]
		public async Task<IActionResult> Index(int id = 0, int pageId = 1) {
			var viewModel = await TopicService.IndexPage(id, pageId);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Display(int id, int pageId = 1, int target = 0) {
			ViewData["Smileys"] = await SmileyService.GetSelectorList();

			var viewModel = await TopicService.DisplayPage(id, pageId, target);

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