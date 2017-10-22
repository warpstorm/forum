using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forum3.Services {
	using DataModels = Models.DataModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	public class SiteSettingsService {
		Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

		DataModels.ApplicationDbContext DbContext { get; }

		public SiteSettingsService(
			DataModels.ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public async Task<ViewModels.IndexPage> IndexPage() {
			var viewModel = new ViewModels.IndexPage();

			var siteSettings = await DbContext.SiteSettings.ToListAsync();

			foreach (var item in siteSettings) {
				viewModel.Items.Add(new ViewModels.IndexItem {
				});
			}

			return viewModel;
		}

		public async Task<string> Get(string name, string userId = "") {
			return await GetSetting(name, userId);
		}

		public async Task<int> GetInt(string name, string userId = "") {
			var setting = await GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return 0;

			return Convert.ToInt32(setting);
		}

		public async Task<bool> GetBool(string name, string userId = "") {
			var setting = await GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return false;

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
							Settings.Add(name, setting == null ? "" : setting.Value);
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