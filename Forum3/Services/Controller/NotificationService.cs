using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Forum3.Controllers;
using Forum3.Enums;
using Forum3.Helpers;

using DataModels = Forum3.Models.DataModels;
using ServiceModels = Forum3.Models.ServiceModels;
using ViewModels = Forum3.Models.ViewModels.Notifications;

namespace Forum3.Services.Controller {
	public class NotificationService {
		DataModels.ApplicationDbContext DbContext { get; }
		ServiceModels.ContextUser ContextUser { get; }
		IUrlHelper UrlHelper { get; }

		public NotificationService(
			DataModels.ApplicationDbContext dbContext,
			ContextUserFactory contextUserFactory,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			ContextUser = contextUserFactory.GetContextUser();
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<ViewModels.Pages.IndexPage> IndexPage(bool showRead = false) {
			var notifications = await GetNotifications(showRead);

			var viewModel = new ViewModels.Pages.IndexPage {
				Notifications = notifications
			};

			return viewModel;
		}

		public async Task<List<ViewModels.Items.IndexItem>> GetNotifications(bool showRead = false) {
			var hiddenTimeLimit = DateTime.Now.AddDays(-7);
			var recentTimeLimit = DateTime.Now.AddMinutes(-30);

			// TODO - validate the indexes on these columns.

			var notificationQuery = from n in DbContext.Notifications
									join targetUser in DbContext.Users on n.TargetUserId equals targetUser.Id into targetUsers
									from targetUser in targetUsers.DefaultIfEmpty()
									where n.Time > hiddenTimeLimit
									where n.UserId == ContextUser.ApplicationUser.Id
									where showRead || n.Unread
									orderby n.Time descending
									select new ViewModels.Items.IndexItem {
										Id = n.Id,
										Type = n.Type,
										Recent = n.Time > recentTimeLimit, 
										Time = n.Time.ToPassedTimeString(),
										TargetUser = targetUser == null ? "User" : targetUser.DisplayName
									};

			var notifications = await notificationQuery.ToListAsync();

			foreach (var notification in notifications)
				notification.Text = NotificationText(notification);

			return notifications;
		}

		public async Task<ServiceModels.ServiceResponse> Open(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var recordQuery = from n in DbContext.Notifications
							  where n.UserId == ContextUser.ApplicationUser.Id
							  where n.Id == id
							  select n;

			var record = await recordQuery.SingleOrDefaultAsync();

			if (record.Unread) {
				record.Unread = false;
				DbContext.Entry(record).State = EntityState.Modified;
				await DbContext.SaveChangesAsync();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.MessageId });
			return serviceResponse;
		}

		string NotificationText(ViewModels.Items.IndexItem notification) {
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