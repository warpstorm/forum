using Forum3.Annotations;
using Forum3.Processes.Topics;
using Forum3.Services.Controller;
using Forum3.ViewModelProviders.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Topics : ForumController {
		MessageService MessageService { get; }
		SmileyService SmileyService { get; }

		public Topics(
			MessageService messageService,
			SmileyService smileyService
		) {
			MessageService = messageService;
			SmileyService = smileyService;
		}

		[HttpGet]
		public IActionResult Index(
			[FromServices] IndexPage pageProvider,
			int id = 0,
			int unread = 0
		) {
			var viewModel = pageProvider.Generate(id, unread);
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult IndexMore(
			[FromServices] IndexMorePage pageProvider,
			int id = 0,
			long after = 0,
			int unread = 0
		) {
			var viewModel = pageProvider.Generate(id, after, unread);
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Display(
			[FromServices] DisplayPage pageProvider,
			int id,
			int pageId = 1,
			int target = 0,
			bool rebuild = false
		) {
			ViewData["Smileys"] = SmileyService.GetSelectorList();

			var viewModel = pageProvider.Generate(id, pageId, target, rebuild);

			if (string.IsNullOrEmpty(viewModel.RedirectPath))
				return View(viewModel);
			else
				return Redirect(viewModel.RedirectPath);
		}

		[HttpGet]
		public IActionResult Latest(
			[FromServices] LatestTopic process,
			int id
		) {
			var serviceResponse = process.Get(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult Admin(InputModels.Continue input = null) => View();

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult RebuildThreadRelationships(
			[FromServices] RebuildThreadRelationships process,
			InputModels.Continue input
		) {
			ViewModels.Delay viewModel;

			if (string.IsNullOrEmpty(input.Stage))
				viewModel = process.Start();
			else
				viewModel = process.Continue(input);

			return View("Delay", viewModel);
		}

		[HttpGet]
		public IActionResult Pin(
			[FromServices] PinTopic process,
			int id
		) {
			var serviceResponse = process.Execute(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult ToggleBoard(
			[FromServices] ToggleBoard process,
			InputModels.ToggleBoardInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = process.Execute(input);
				ProcessServiceResponse(serviceResponse);
			}

			return new NoContentResult();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(
			[FromServices] DisplayPage pageProvider,
			InputModels.MessageInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.CreateReply(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = pageProvider.Generate(input.Id);
			viewModel.ReplyForm.Body = input.Body;

			return View(nameof(Display), viewModel);
		}
	}
}