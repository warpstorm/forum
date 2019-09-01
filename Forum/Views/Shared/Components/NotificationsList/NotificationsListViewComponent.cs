using Forum.Models.Options;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Views.Shared.Components.NotificationsList {
	public class NotificationsListViewComponent : ViewComponent {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }

		public NotificationsListViewComponent(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
		}

		public async Task<IViewComponentResult> InvokeAsync() {
			if (UserContext.ApplicationUser is null) {
				throw new Exception("ApplicationUser is null or not authenticated.");
			}

			var hiddenTimeLimit = DateTime.Now.AddDays(-7);
			var recentTimeLimit = DateTime.Now.AddMinutes(-30);

			var notificationQuery = from n in DbContext.Notifications
									where n.UserId == UserContext.ApplicationUser.Id
									where n.Time > hiddenTimeLimit
									where n.Unread
									orderby n.Time descending
									select new DisplayItem {
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
					_ => $"Unknown type.",
				};
			}

			return View("Default", notifications);
		}

		public class DisplayItem {
			public int Id { get; set; }
			public string TargetUserId { get; set; }
			public string TargetUser { get; set; }
			public string Text { get; set; }
			public DateTime Time { get; set; }
			public ENotificationType Type { get; set; }
			public bool Recent { get; set; }
		}
	}
}
