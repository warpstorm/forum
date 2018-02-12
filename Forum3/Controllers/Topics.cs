using Forum3.Annotations;
using Forum3.Interfaces;
using Forum3.Processes;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Topics : ForumController {
		TopicService TopicService { get; }
		MessageService MessageService { get; }
		SmileyService SmileyService { get; }
		Func<Type, IControllerProcess> ControllerProcessFactory { get; }

		public Topics(
			TopicService topicService,
			MessageService messageService,
			SmileyService smileyService,
			Func<Type, IControllerProcess> controllerProcessFactory
		) {
			TopicService = topicService;
			MessageService = messageService;
			SmileyService = smileyService;
			ControllerProcessFactory = controllerProcessFactory;
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
		public IActionResult Display(int id, int pageId = 1, int target = 0, bool rebuild = false) {
			ViewData["Smileys"] = SmileyService.GetSelectorList();

			var viewModel = TopicService.DisplayPage(id, pageId, target, rebuild);

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

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult Admin(InputModels.Continue input = null) => View();

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult RebuildThreadRelationships(InputModels.Continue input) {
			var process = ControllerProcessFactory(typeof(RebuildThreadRelationshipsProcess));

			ViewModels.Delay viewModel;

			if (string.IsNullOrEmpty(input.Stage))
				viewModel = process.Start();
			else
				viewModel = process.Continue(input);

			return View("Delay", viewModel);
		}

		[HttpGet]
		public IActionResult Pin(int id) {
			var serviceResponse = TopicService.Pin(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicService.ToggleBoard(input);
				ProcessServiceResponse(serviceResponse);
			}

			return new NoContentResult();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(InputModels.MessageInput input) {
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