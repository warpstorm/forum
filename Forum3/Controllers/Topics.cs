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
		public IActionResult Index(int id = 0, int unread = 0) {
			var viewModel = TopicService.IndexPage(id, unread);
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult IndexMore(int id = 0, long after = 0, int unread = 0) {
			var viewModel = TopicService.IndexMore(id, after, unread);
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Display(int id, int pageId = 1, int target = 0) {
			ViewData["Smileys"] = SmileyService.GetSelectorList();

			var viewModel = TopicService.DisplayPage(id, pageId, target);

			if (string.IsNullOrEmpty(viewModel.RedirectPath))
				return View(viewModel);
			else
				return Redirect(viewModel.RedirectPath);
		}

		[HttpGet]
		public IActionResult Latest(int id) {
			var serviceResponse = TopicService.Latest(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult Pin(int id) {
			var serviceResponse = TopicService.Pin(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult ToggleBoard(ToggleBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicService.ToggleBoard(input);
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

			var viewModel = TopicService.DisplayPage(input.Id);
			viewModel.ReplyForm.Body = input.Body;

			return View(nameof(Display), viewModel);
		}
	}
}