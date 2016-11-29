using System.Threading.Tasks;
using Forum3.ViewModels.Messages;
using Forum3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Forum3.Controllers {
	[Authorize]
	public class MessagesController : Controller {
		MessageService Messages { get; }

		public MessagesController(MessageService messageService) {
			Messages = messageService;
		}

		public IActionResult Create() {
			return View(new TopicFirstPost { FormAction = "Create" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(TopicFirstPost input) {
			if (ModelState.IsValid) {
				await Messages.CreateAsync(input.Body);
				return RedirectToAction("Index", "Topics");
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(EditPost input) {
			if (ModelState.IsValid) {
				await Messages.UpdateAsync(input.Id, input.Body);
				return RedirectToAction("Display", "Topics", new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> TopicReply(TopicReplyPost input) {
			if (ModelState.IsValid) {
				await Messages.CreateAsync(input.Body, parentId: input.Id);
				return RedirectToAction("Display", "Topics", new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DirectReply(DirectReplyPost input) {
			if (ModelState.IsValid) {
				await Messages.CreateAsync(input.Body, replyId: input.Id);
				return RedirectToAction("Display", "Topics", new { Id = input.Id });
			}

			return View(input);
		}

		public async Task<IActionResult> Delete(int id) {
			await Messages.DeleteAsync(id);
			return RedirectToAction("Index", "Topics");
		}
	}
}
