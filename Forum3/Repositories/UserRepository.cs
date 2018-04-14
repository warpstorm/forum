using Forum3.Contexts;
using Forum3.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using ItemViewModels = Models.ViewModels.Boards.Items;

	public class UserRepository {
		public List<DataModels.ApplicationUser> All { get; }

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		SettingsRepository SettingsRepository { get; }

		public UserRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			SettingsRepository settingsRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			SettingsRepository = settingsRepository;

			All = DbContext.Users.ToList();
		}

		public List<string> GetBirthdaysList() {
			var todayBirthdayNames = new List<string>();

			var birthdays = All.Select(u => new {
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
			var onlineTimeLimitSetting = SettingsRepository.OnlineTimeLimit(UserContext.ApplicationUser.Id);
			onlineTimeLimitSetting *= -1;

			var onlineTimeLimit = DateTime.Now.AddMinutes(onlineTimeLimitSetting);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsersQuery = from user in All
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