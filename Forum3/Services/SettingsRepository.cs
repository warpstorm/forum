using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forum3.Services {
	using DataModels = Models.DataModels;

	public class SettingsRepository {
		DataModels.ApplicationDbContext DbContext { get; }

		Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();
		Dictionary<string, Dictionary<string, string>> UserSettings { get; set; } = new Dictionary<string, Dictionary<string, string>>();

		public SettingsRepository(
			DataModels.ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public async Task<int> AvatarSize() {
			var setting = await GetInt(Constants.Settings.AvatarSize);

			if (setting == 0)
				setting = 100;

			return setting;
		}

		public async Task<int> HistoryTimeLimit() {
			var setting = await GetInt(Constants.Settings.HistoryTimeLimit);

			if (setting == 0)
				setting = -14;

			return setting;
		}

		public async Task<int> MessagesPerPage() {
			var setting = await GetInt(Constants.Settings.MessagesPerPage);

			if (setting == 0)
				setting = 15;

			return setting;
		}

		public async Task<int> OnlineTimeLimit() {
			var setting = await GetInt(Constants.Settings.OnlineTimeLimit);

			if (setting == 0)
				setting = 5;

			return setting;
		}

		public async Task<int> PopularityLimit() {
			var setting = await GetInt(Constants.Settings.PopularityLimit);

			if (setting == 0)
				setting = 25;

			return setting;
		}

		public async Task<int> TopicsPerPage() {
			var setting = await GetInt(Constants.Settings.TopicsPerPage);

			if (setting == 0)
				setting = 15;

			return setting;
		}

		public async Task<string> GetSetting(string name, string userId = "") {
			DataModels.SiteSetting setting = null;
			var settingValue = string.Empty;

			if (string.IsNullOrEmpty(userId)) {
				if (!Settings.ContainsKey(name)) {
					setting = await DbContext.SiteSettings.SingleOrDefaultAsync(r => r.Name == name && string.IsNullOrEmpty(r.UserId));

					lock (Settings) {
						if (!Settings.ContainsKey(name))
							Settings.Add(name, setting == null ? string.Empty : setting.Value);
					}
				}

				Settings.TryGetValue(name, out settingValue);
			}
			else {
				if (!UserSettings.ContainsKey(userId)) {
					lock (UserSettings) {
						UserSettings.TryAdd(userId, new Dictionary<string, string>());
					}
				}

				if (!UserSettings[userId].ContainsKey(name)) {
					setting = await DbContext.SiteSettings.SingleOrDefaultAsync(r => r.Name == name && r.UserId == userId);

					lock (UserSettings[userId]) {
						if (!UserSettings[userId].ContainsKey(name))
							UserSettings[userId].Add(name, setting == null ? string.Empty : setting.Value);
					}
				}

				UserSettings[userId].TryGetValue(name, out settingValue);
			}

			return settingValue;
		}

		async Task<int> GetInt(string name, string userId = "") {
			var setting = await GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(int);

			return Convert.ToInt32(setting);
		}

		async Task<bool> GetBool(string name, string userId = "") {
			var setting = await GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(bool);

			return Convert.ToBoolean(setting);
		}
	}
}