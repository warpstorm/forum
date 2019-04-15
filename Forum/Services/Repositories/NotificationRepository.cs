using Forum.Models.Options;
using Forum.Services.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using ViewModels = Models.ViewModels;

	public class NotificationRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }

		public NotificationRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
		}

		public async Task<List<ViewModels.Notifications.IndexItem>> Index(bool showRead = false) {
			if (UserContext.ApplicationUser is null) {
				return new List<ViewModels.Notifications.IndexItem>();
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

				notification.Text = NotificationText(notification);
			}

			return notifications;
		}

		public string NotificationText(ViewModels.Notifications.IndexItem notification) {
			switch (notification.Type) {
				case ENotificationType.Quote:
					return $"{notification.TargetUser} quoted you.";
				case ENotificationType.Reply:
					return $"{notification.TargetUser} replied to your topic.";
				case ENotificationType.Thought:
					return $"{notification.TargetUser} thought about your post.";
				case ENotificationType.Mention:
					return $"{notification.TargetUser} mentioned you.";
				default:
					return $"Unknown notification type. {notification.Id} | {notification.Type}";
			}
		}
	}
}