using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forum3.Services {
	using DataModels = Models.DataModels;

	public class SiteSettingsRepository {
		Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

		DataModels.ApplicationDbContext DbContext { get; }

		public SiteSettingsRepository(
			DataModels.ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public async Task<string> Get(string name, string userId = "") => await GetSetting(name, userId);

		public async Task<int> GetInt(string name, string userId = "") {
			var setting = await GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(int);

			return Convert.ToInt32(setting);
		}

		public async Task<bool> GetBool(string name, string userId = "") {
			var setting = await GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(bool);

			return Convert.ToBoolean(setting);
		}

		async Task<string> GetSetting(string name, string userId = "") {
			DataModels.SiteSetting setting = null;
			var settingValue = string.Empty;

			if (string.IsNullOrEmpty(userId)) {
				if (!Settings.ContainsKey(name)) {
					setting = await DbContext.SiteSettings.FirstOrDefaultAsync(r => r.Name == name && r.UserId == userId);

					lock (Settings) {
						if (!Settings.ContainsKey(name))
							Settings.Add(name, setting == null ? string.Empty : setting.Value);
					}
				}

				Settings.TryGetValue(name, out settingValue);
			}
			else {
				setting = await DbContext.SiteSettings.FirstOrDefaultAsync(r => r.Name == name && r.UserId == userId);

				if (setting != null)
					settingValue = setting.Value;
			}

			return settingValue;
		}
	}
}