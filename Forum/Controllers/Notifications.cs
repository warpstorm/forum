using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Models.Errors;
using Forum.Models.Options;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels;

	public class Notifications : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }

		public Notifications(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
		}

		[ActionLog("is viewing their notifications.")]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read")) {
				showRead = true;
			}

			if (UserContext.ApplicationUser is null) {
				throw new Exception("ApplicationUser is null or not authenticated.");
			}

			var hiddenTimeLimit = DateTime.Now.AddDays(-7);
			var recentTimeLimit = DateTime.Now.AddMinutes(-30);

			var notificationQuery = from n in DbContext.Notifications
									where n.UserId == UserContext.ApplicationUser.Id
									where n.Time > hiddenTimeLimit
									where showRead || n.Unread
									orderby n.Time descending
									select new ViewModels.Notifications.IndexItem {
										Id = n.Id,
										Type = n.Type,
										Recent = n.Time > recentTimeLimit,
										Time = n.Time,
										TargetUserId = n.TargetUserId
									};

			var notifications = notificationQuery.ToList();
			var users = await AccountRepository.Records();

			foreach (var notification in notifications) {
				if (!string.IsNullOrEmpty(notification.TargetUserId)) {
					notification.TargetUser = users.FirstOrDefault(r => r.Id == notification.TargetUserId)?.DecoratedName ?? "User";
				}

				notification.Text = notification.Type switch {
					ENotificationType.Quote => $"{notification.TargetUser} quoted you.",
					ENotificationType.Reply => $"{notification.TargetUser} replied to your topic.",
					ENotificationType.Thought => $"{notification.TargetUser} thought about your post.",
					ENotificationType.Mention => $"{notification.TargetUser} mentioned you.",
					_ => $"Unknown notification type. {notification.Id} | {notification.Type}"
				};
			}

			var viewModel = new ViewModels.Notifications.IndexPage {
				Notifications = notifications
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Open(int id) {
			if (ModelState.IsValid) {
				var recordQuery = from n in DbContext.Notifications
								  where n.UserId == UserContext.ApplicationUser.Id
								  where n.Id == id
								  select n;

				var record = await recordQuery.FirstOrDefaultAsync();

				if (record is null) {
					throw new HttpNotFoundError();
				}

				if (record.Unread) {
					record.Unread = false;
					DbContext.Update(record);
					await DbContext.SaveChangesAsync();
				}

				var message = await DbContext.Messages.FindAsync(record.MessageId);
				var topicId = message.TopicId;

				var redirectPath = Url.Action(nameof(Topics.Display), nameof(Topics), new { id = topicId, page = 1, target = record.MessageId }) + $"#message{record.MessageId}";
				return Redirect(redirectPath);
			}

			return this.RedirectToReferrer();
		}

		[HttpGet]
		public ActionResult MarkAllRead() => Redirect("/");
	}
}