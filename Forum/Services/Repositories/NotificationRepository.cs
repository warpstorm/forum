using Forum.Services.Contexts;
using Forum.Controllers;
using Forum.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.Notifications;

	public class NotificationRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		IUrlHelper UrlHelper { get; }

		public NotificationRepository (
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<List<ViewModels.Items.IndexItem>> Index(bool showRead = false) {
			if (UserContext.ApplicationUser is null) {
				return new List<ViewModels.Items.IndexItem>();
			}

			var hiddenTimeLimit = DateTime.Now.AddDays(-7);
			var recentTimeLimit = DateTime.Now.AddMinutes(-30);

			var notificationQuery = from n in DbContext.Notifications
									where n.UserId == UserContext.ApplicationUser.Id
									where n.Time > hiddenTimeLimit
									where showRead || n.Unread
									orderby n.Time descending
									select new ViewModels.Items.IndexItem {
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

		public ServiceModels.ServiceResponse Open(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var recordQuery = from n in DbContext.Notifications
							  where n.UserId == UserContext.ApplicationUser.Id
							  where n.Id == id
							  select n;

			var record = recordQuery.FirstOrDefault();

			if (record is null) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Notifications.Index), nameof(Notifications));
				return serviceResponse;
			}

			if (record.Unread) {
				record.Unread = false;
				DbContext.Update(record);
				DbContext.SaveChanges();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.MessageId });
			return serviceResponse;
		}

		public string NotificationText(ViewModels.Items.IndexItem notification) {
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