using Forum3.Contexts;
using Forum3.Extensions;
using Forum3.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using ItemViewModels = Models.ViewModels.Boards.Items;

	public class UserRepository {
		ApplicationDbContext DbContext { get; }
		SettingsRepository Settings { get; }

		public UserRepository(
			ApplicationDbContext dbContext,
			SettingsRepository SettingsRepository
		) {
			DbContext = dbContext;
			Settings = SettingsRepository;
		}

		public List<string> GetBirthdaysList() {
			var todayBirthdayNames = new List<string>();

			var birthdays = DbContext.Users.Select(u => new {
				u.Birthday,
				u.DisplayName
			}).ToList();

			if (birthdays.Any()) {
				var todayBirthdays = birthdays.Where(u => new DateTime(DateTime.Now.Year, u.Birthday.Month, u.Birthday.Day).Date == DateTime.Now.Date);

				foreach (var item in todayBirthdays) {
					var now = DateTime.Today;
					var age = now.Year - item.Birthday.Year;

					if (item.Birthday > now.AddYears(-age))
						age--;

					todayBirthdayNames.Add($"{item.DisplayName} ({age})");
				}
			}

			return todayBirthdayNames;
		}

		public List<ItemViewModels.OnlineUser> GetOnlineList() {
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