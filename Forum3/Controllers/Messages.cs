using Forum3.Annotations;
using Forum3.Models.InputModels;
using Forum3.Models.ViewModels.Messages;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
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
			viewModel.CancelPath = Referrer;

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.CreateTopic(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
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
			viewModel.CancelPath = Referrer;

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageService.EditMessage(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = new CreateTopicPage() {
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			var serviceResponse = await MessageService.DeleteMessage(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> AddThought(ThoughtInput input) {
			var serviceResponse = await MessageService.AddThought(input);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public async Task<IActionResult> Migrate(int id) {
			var viewModel = await MessageService.MigratePage(id);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> FinishMigration(int id) {
			var serviceResponse = await MessageService.FinishMigration(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}
	}
}
