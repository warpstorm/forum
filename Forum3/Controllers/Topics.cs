using Forum3.Annotations;
using Forum3.Models.InputModels;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
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

		[HttpGet]
		public async Task<IActionResult> Pin(int id) {
			var serviceResponse = await TopicService.Pin(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public async Task<IActionResult> ToggleBoard(ToggleBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await TopicService.ToggleBoard(input);
				ProcessServiceResponse(serviceResponse);
			}

			return new NoContentResult();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.CreateReply(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = await TopicService.DisplayPage(input.Id);
			viewModel.ReplyForm.Body = input.Body;

			return View(nameof(Display), viewModel);
		}
	}
}