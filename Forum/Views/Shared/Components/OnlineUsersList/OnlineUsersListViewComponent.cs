using Forum.Controllers.Annotations;
using Forum.Data.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Views.Shared.Components.OnlineUsersList {
	using DataModels = Data.Models;

	public class OnlineUsersListViewComponent : ViewComponent {
		ApplicationDbContext DbContext { get; }
		AccountRepository AccountRepository { get; }
		IUrlHelper UrlHelper { get; }

		public OnlineUsersListViewComponent(
			ApplicationDbContext dbContext,
			AccountRepository accountRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			AccountRepository = accountRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<IViewComponentResult> InvokeAsync() {
			// Users are considered "offline" after 5 minutes.
			var onlineTimeLimit = DateTime.Now.AddMinutes(-5);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsersQuery = from user in await AccountRepository.Records()
								   where user.LastOnline >= onlineTodayTimeLimit
								   orderby user.LastOnline descending
								   select new {
									   user.Id,
									   user.DecoratedName,
									   user.LastOnline,
									   user.LastActionLogItemId
								   };

			var onlineUsers = new List<DisplayItem>();

			foreach (var user in onlineUsersQuery) {
				var lastActionItem = await DbContext.ActionLog.FindAsync(user.LastActionLogItemId);

				onlineUsers.Add(new DisplayItem {
					Id = user.Id,
					Name = user.DecoratedName,
					LastOnline = user.LastOnline,
					IsOnline = user.LastOnline > onlineTimeLimit,
					LastActionText = actionLogItemText(lastActionItem),
					LastActionUrl = actionLogItemUrl(lastActionItem)
				});
			}

			return View("Default", onlineUsers);

			string actionLogItemUrl(DataModels.ActionLogItem logItem) => logItem is null ? string.Empty : UrlHelper.Action(logItem.Action, logItem.Controller, logItem.Arguments);

			string actionLogItemText(DataModels.ActionLogItem logItem) {
				if (!(logItem is null)) {
					var controller = Type.GetType($"Forum.Controllers.{logItem.Controller}");

					foreach (var method in controller.GetMethods()) {
						if (method.Name == logItem.Action) {
							var attribute = method.GetCustomAttributes(typeof(ActionLogAttribute), false).FirstOrDefault() as ActionLogAttribute;

							if (!(attribute is null)) {
								return attribute.Description;
							}
						}
					}
				}

				return string.Empty;
			}
		}

		public class DisplayItem {
			public string Id { get; set; }
			public string Name { get; set; }
			public DateTime LastOnline { get; set; }
			public bool IsOnline { get; set; }
			public int LastActionLogItemId { get; set; }
			public string LastActionUrl { get; set; }
			public string LastActionText { get; set; }
		}
	}
}
