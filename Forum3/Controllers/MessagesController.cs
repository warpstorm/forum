using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Forum3.ViewModels.Messages;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize]
	public class MessagesController : Controller {
		private MessageRepository _messages;

		public MessagesController(MessageRepository messageRepo) {
			_messages = messageRepo;
		}

		// POST: Messages/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Input input) {
			if (ModelState.IsValid) {
				await _messages.CreateAsync(input.Body);
				return RedirectToAction("Index");
			}

			return View(input);
		}

		// POST: Messages/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Input input) {
			if (ModelState.IsValid) {
				await _messages.UpdateAsync((int) input.Id, input.Body);
				return RedirectToAction("Index");
			}

			return View(input);
		}
		
		// GET: Messages/Delete/5
		public async Task<IActionResult> Delete(int id) {
			await _messages.DeleteAsync(id);
			return RedirectToAction("Index");
		}
	}
}
