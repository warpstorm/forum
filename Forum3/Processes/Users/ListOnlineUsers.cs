using Forum3.Contexts;
using Forum3.Extensions;
using Forum3.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Processes.Users {
	using ItemViewModels = Models.ViewModels.Boards.Items;

	public class ListOnlineUsers {
		ApplicationDbContext DbContext { get; }
		SettingsRepository Settings { get; }

		public ListOnlineUsers(
			ApplicationDbContext dbContext,
			SettingsRepository SettingsRepository
		) {
			DbContext = dbContext;
			Settings = SettingsRepository;
		}

		public List<ItemViewModels.OnlineUser> Execute() {
			var onlineTimeLimitSetting = Settings.OnlineTimeLimit();
			onlineTimeLimitSetting *= -1;

			var onlineTimeLimit = DateTime.Now.AddMinutes(onlineTimeLimitSetting);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsersQuery = from user in DbContext.Users
								   where user.LastOnline >= onlineTodayTimeLimit
								   orderby user.LastOnline descending
								   select new ItemViewModels.OnlineUser {
									   Id = user.Id,
									   Name = user.DisplayName,
									   Online = user.LastOnline >= onlineTimeLimit,
									   LastOnline = user.LastOnline
								   };

			var onlineUsers = onlineUsersQuery.ToList();

			foreach (var onlineUser in onlineUsers)
				onlineUser.LastOnlineString = onlineUser.LastOnline.ToPassedTimeString();

			return onlineUsers;
		}
	}
}