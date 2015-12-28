using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Forum3.Data;
using Forum3.ViewModels.Messages;
using Forum3.Services;
using System.Security.Claims;

namespace Forum3.Controllers {
	[Authorize]
	public class MessagesController : Controller {
		private ApplicationDbContext _dbContext;
		private MessageService _messageService;

		public MessagesController(ApplicationDbContext dbContext, MessageService messageService) {
			_dbContext = dbContext;
			_messageService = messageService;
		}

		// POST: Messages/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Input input) {
			if (ModelState.IsValid) {
				var processedMessageBody = await _messageService.ProcessAsync(input.Body);

				var userId = User.GetUserId();
				var userProfile = await _dbContext.Users.SingleAsync(u => u.Id == userId);

				var newRecord = new DataModels.Message {
					OriginalBody = processedMessageBody.OriginalBody,
					DisplayBody = processedMessageBody.DisplayBody,
					ShortPreview = processedMessageBody.ShortPreview,
					LongPreview = processedMessageBody.LongPreview,

					TimePosted = DateTime.Now,
					PostedById = userId,
					PostedByName = userProfile.DisplayName,

					TimeEdited = DateTime.Now,
					EditedById = userId,
					EditedByName = userProfile.DisplayName
				};

				_dbContext.Messages.Add(newRecord);

				await _dbContext.SaveChangesAsync();

				return RedirectToAction("Index");
			}

			return View(input);
		}

		// POST: Messages/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(DataModels.Message message) {
			if (ModelState.IsValid) {
				_dbContext.Update(message);

				await _dbContext.SaveChangesAsync();
				return RedirectToAction("Index");
			}

			return View(message);
		}
		
		// GET: Messages/Delete/5
		public async Task<IActionResult> Delete(int id) {
			var message = await _dbContext.Messages.SingleAsync(m => m.Id == id);

			_dbContext.Messages.Remove(message);

			await _dbContext.SaveChangesAsync();
			return RedirectToAction("Index");
		}
	}
}
