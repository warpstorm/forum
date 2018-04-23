using Forum3.Contexts;
using Forum3.Controllers;
using Forum3.Enums;
using Forum3.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.Notifications;

	public class NotificationRepository {
		public List<DataModels.Notification> ForCurrentUser {
			get {
				if (_ForCurrentUser is null) {
					var notificationQuery = from n in DbContext.Notifications
											where n.UserId == UserContext.ApplicationUser.Id
											select n;

					_ForCurrentUser = notificationQuery.ToList();
				}

				return _ForCurrentUser;
			}
		}
		List<DataModels.Notification> _ForCurrentUser;

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

		public List<ViewModels.Items.IndexItem> Index(bool showRead = false) {
			if (UserContext.ApplicationUser is null)
				return new List<ViewModels.Items.IndexItem>();

			var hiddenTimeLimit = DateTime.Now.AddDays(-7);
			var recentTimeLimit = DateTime.Now.AddMinutes(-30);

			var notificationQuery = from n in ForCurrentUser
									join targetUser in DbContext.Users on n.TargetUserId equals targetUser.Id into targetUsers
									from targetUser in targetUsers.DefaultIfEmpty()
									where n.Time > hiddenTimeLimit
									where showRead || n.Unread
									orderby n.Time descending
									select new ViewModels.Items.IndexItem {
										Id = n.Id,
										Type = n.Type,
										Recent = n.Time > recentTimeLimit,
										Time = n.Time.ToPassedTimeString(),
										TargetUser = targetUser == null ? "User" : targetUser.DisplayName
									};

			var notifications = notificationQuery.ToList();

			foreach (var notification in notifications)
				notification.Text = NotificationText(notification);

			return notifications;
		}

		public ServiceModels.ServiceResponse Open(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var recordQuery = from n in ForCurrentUser
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