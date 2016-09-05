using System.Threading.Tasks;
using Forum3.ViewModels.Messages;
using Forum3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Forum3.Controllers {
	[Authorize]
	public class MessagesController : Controller {
		private MessageRepository _messages;

		public MessagesController(MessageRepository messageRepo) {
			_messages = messageRepo;
		}

		public IActionResult Create() {
			return View(new Input { FormAction = "Create" });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Input input) {
			if (ModelState.IsValid) {
				await _messages.CreateAsync(input.Body);
				return RedirectToAction("Index", "Topics");
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Input input) {
			if (ModelState.IsValid) {
				await _messages.UpdateAsync(input.Id, input.Body);
				return RedirectToAction("Display", "Topics", new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> TopicReply(Input input) {
			if (ModelState.IsValid) {
				await _messages.CreateAsync(input.Body, parentId: input.Id);
				return RedirectToAction("Display", "Topics", new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DirectReply(Input input) {
			if (ModelState.IsValid) {
				await _messages.CreateAsync(input.Body, replyId: input.Id);
				return RedirectToAction("Display", "Topics", new { Id = input.Id });
			}

			return View(input);
		}

		public async Task<IActionResult> Delete(int id) {
			await _messages.DeleteAsync(id);
			return RedirectToAction("Index", "Topics");
		}
	}
}
