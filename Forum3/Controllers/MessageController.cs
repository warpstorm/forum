using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Forum3.Models;
using System;
using Forum3.ViewModels.Message;

namespace Forum3.Controllers {
	public class MessageController : Controller {
		private ApplicationDbContext _context;

		public MessageController(ApplicationDbContext context) {
			_context = context;
		}

		// GET: Message
		public async Task<IActionResult> Index() {
			var skip = 0;

			if (HttpContext.Request.Query.ContainsKey("skip"))
				skip = Convert.ToInt32(HttpContext.Request.Query["skip"]);

			var take = 15;

			if (HttpContext.Request.Query.ContainsKey("take"))
				take = Convert.ToInt32(HttpContext.Request.Query["take"]);

			var messageRecords = _context.Messages.Where(m => m.ParentId == 0).OrderByDescending(m => m.LastReplyPosted);

			var topicList = await messageRecords.Select(m => new Topic {
				Id = m.Id,
				Subject = m.ShortPreview,
				LastReplyId = m.LastReplyId,
				LastReplyById = m.LastReplyById,
				LastReplyPostedDT = m.LastReplyPosted,
				Views = m.Views,
				Replies = m.Replies,
			}).ToListAsync();

			var skipped = 0;
			var viewModel = new TopicIndex {
				Skip = skip + take,
				Take = take
			};

			foreach (var topic in topicList) {
				if (viewModel.Topics.Count() > take) {
					viewModel.MoreMessages = true;
					break;
				}

				if (skipped < skip) {
					skipped++;
					continue;
				}

				viewModel.Topics.Add(topic);
			}

			return View(viewModel);
		}

		// GET: Message/Create
		public IActionResult Create() {
			return View();
		}

		// POST: Message/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Input input) {
			if (ModelState.IsValid) {
				var newRecord = new Message {
					OriginalBody = input.Body
				};

				_context.Messages.Add(newRecord);

				await _context.SaveChangesAsync();

				return RedirectToAction("Index");
			}

			return View(input);
		}

		// GET: Message/Edit/5
		public async Task<IActionResult> Edit(int? id) {
			if (id == null)
				return HttpNotFound();

			var message = await _context.Messages.SingleAsync(m => m.Id == id);

			if (message == null)
				return HttpNotFound();

			return View(message);
		}

		// POST: Message/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Message message) {
			if (ModelState.IsValid) {
				_context.Update(message);

				await _context.SaveChangesAsync();
				return RedirectToAction("Index");
			}

			return View(message);
		}
		
		// GET: Message/Delete/5
		public async Task<IActionResult> Delete(int id) {
			var message = await _context.Messages.SingleAsync(m => m.Id == id);

			_context.Messages.Remove(message);

			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}
	}
}
