using Forum3.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Services {
	using DataModels = Models.DataModels;

	public class SettingsRepository {
		ApplicationDbContext DbContext { get; }

		Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();
		Dictionary<string, Dictionary<string, string>> UserSettings { get; } = new Dictionary<string, Dictionary<string, string>>();

		public SettingsRepository(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public int AvatarSize() {
			var setting = GetInt(Constants.Settings.AvatarSize);

			if (setting == 0)
				setting = 100;

			return setting;
		}

		public DateTime HistoryTimeLimit() {
			var setting = GetInt(Constants.Settings.HistoryTimeLimit);

			if (setting == 0)
				setting = -14;

			return DateTime.Now.AddDays(setting);
		}

		public int MessagesPerPage() {
			var setting = GetInt(Constants.Settings.MessagesPerPage);

			if (setting == 0)
				setting = 15;

			return setting;
		}

		public int OnlineTimeLimit() {
			var setting = GetInt(Constants.Settings.OnlineTimeLimit);

			if (setting == 0)
				setting = 5;

			return setting;
		}

		public int PopularityLimit() {
			var setting = GetInt(Constants.Settings.PopularityLimit);

			if (setting == 0)
				setting = 25;

			return setting;
		}

		public int TopicsPerPage() {
			var setting = GetInt(Constants.Settings.TopicsPerPage);

			if (setting == 0)
				setting = 15;

			return setting;
		}

		public string GetSetting(string name, string userId = "") {
			DataModels.SiteSetting setting = null;
			var settingValue = string.Empty;

			if (string.IsNullOrEmpty(userId)) {
				if (!Settings.ContainsKey(name)) {
					setting = DbContext.SiteSettings.Where(r => r.Name == name && string.IsNullOrEmpty(r.UserId)).FirstOrDefault();

					lock (Settings) {
						if (!Settings.ContainsKey(name))
							Settings.Add(name, setting is null ? string.Empty : setting.Value);
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
					setting = DbContext.SiteSettings.Where(r => r.Name == name && r.UserId == userId).FirstOrDefault();

					lock (UserSettings[userId]) {
						if (!UserSettings[userId].ContainsKey(name))
							UserSettings[userId].Add(name, setting is null ? string.Empty : setting.Value);
					}
				}

				UserSettings[userId].TryGetValue(name, out settingValue);
			}

			return settingValue;
		}

		int GetInt(string name, string userId = "") {
			var setting = GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(int);

			return Convert.ToInt32(setting);
		}

		bool GetBool(string name, string userId = "") {
			var setting = GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(bool);

			return Convert.ToBoolean(setting);
		}
	}
}