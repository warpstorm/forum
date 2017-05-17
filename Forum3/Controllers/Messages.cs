using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Forum3.Annotations;
using Forum3.Models.InputModels;
using Forum3.Services;
using Forum3.Models.ViewModels.Messages;

namespace Forum3.Controllers {
	[Authorize]
	public class Messages : ForumController {
		public MessageService ControllerService { get; }

		public Messages(MessageService controllerService, UserService userService) : base(userService) {
			ControllerService = controllerService;
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			var viewModel = await ControllerService.CreatePage(id);

			viewModel.CancelPath = Request.Headers["Referer"].ToString();

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await ControllerService.CreateTopic(input);
				ProcessServiceResponse(serviceResponse);

				if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
					return Redirect(serviceResponse.RedirectPath);
			}

			var viewModel = new CreateTopicPage();

			viewModel.Body = input.Body;

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var viewModel = await ControllerService.EditPage(id);

			viewModel.CancelPath = Request.Headers["Referer"].ToString();

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await ControllerService.EditMessage(input);
				ProcessServiceResponse(serviceResponse);

				return RedirectToAction(nameof(Topics.Display), nameof(Topics), new { Id = input.Id });
			}

			var viewModel = new CreateTopicPage();
			viewModel.Body = input.Body;

			return View(viewModel);
		}
		
		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(MessageInput input) {
			if (ModelState.IsValid) {
				await ControllerService.CreateReply(input);
				return RedirectToAction(nameof(Topics.Display), nameof(Topics), new { Id = input.Id });
			}

			return View(input);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			await ControllerService.DeleteMessage(id);
			return RedirectToAction(nameof(Topics.Index), nameof(Topics));
		}
	}
}
