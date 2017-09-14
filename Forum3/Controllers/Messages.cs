using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Forum3.Annotations;
using Forum3.Models.InputModels;
using Forum3.Models.ViewModels.Messages;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize]
	public class Messages : ForumController {
		MessageService MessageService { get; }

		public Messages(
			MessageService messageService
		) {
			MessageService = messageService;
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			var viewModel = await MessageService.CreatePage(id);

			viewModel.CancelPath = Request.Headers["Referer"].ToString();

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.CreateTopic(input);
				ProcessServiceResponse(serviceResponse);

				if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
					return Redirect(serviceResponse.RedirectPath);
			}

			var viewModel = new CreateTopicPage() {
				BoardId = input.BoardId,
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var viewModel = await MessageService.EditPage(id);

			viewModel.CancelPath = Request.Headers["Referer"].ToString();

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.EditMessage(input);
				ProcessServiceResponse(serviceResponse);

				if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
					return Redirect(serviceResponse.RedirectPath);
			}

			var viewModel = new CreateTopicPage() {
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			await MessageService.DeleteMessage(id);
			return RedirectToAction(nameof(Topics.Index), nameof(Topics));
		}
	}
}
