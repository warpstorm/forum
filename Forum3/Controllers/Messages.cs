using System.Threading.Tasks;
using Forum3.ViewModels.Messages;
using Forum3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Forum3.Controllers {
	[Authorize]
	public class Messages : Controller {
		MessageService MessageService { get; }

		public Messages(MessageService messageService) {
			MessageService = messageService;
		}

		public IActionResult Create() {
			return View(new TopicFirstPost());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(TopicFirstPost input) {
			if (ModelState.IsValid) {
				await MessageService.Create(input.Body);
				return RedirectToAction(nameof(Topics.Index), nameof(Topics));
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(EditPost input) {
			if (ModelState.IsValid) {
				await MessageService.Update(input.Id, input.Body);
				return RedirectToAction(nameof(Topics.Display), nameof(Topics), new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> TopicReply(TopicReplyPost input) {
			if (ModelState.IsValid) {
				await MessageService.Create(input.Body, parentId: input.Id);
				return RedirectToAction(nameof(Topics.Display), nameof(Topics), new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DirectReply(DirectReplyPost input) {
			if (ModelState.IsValid) {
				await MessageService.Create(input.Body, replyId: input.Id);
				return RedirectToAction(nameof(Topics.Display), nameof(Topics), new { Id = input.Id });
			}

			return View(input);
		}

		public async Task<IActionResult> Delete(int id) {
			await MessageService.Delete(id);
			return RedirectToAction(nameof(Topics.Index), nameof(Topics));
		}
	}
}
