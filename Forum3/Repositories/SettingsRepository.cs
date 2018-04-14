using Forum3.Contexts;
using Forum3.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.SiteSettings;

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
			var settingValue = string.Empty;

			if (!string.IsNullOrEmpty(userId)) {
				if (!UserSettings.ContainsKey(userId)) {
					lock (UserSettings) {
						UserSettings.TryAdd(userId, new Dictionary<string, string>());
					}
				}

				if (!UserSettings[userId].ContainsKey(name)) {
					var setting = DbContext.SiteSettings.Where(r => r.Name == name && r.UserId == userId).FirstOrDefault();

					lock (UserSettings[userId]) {
						if (!UserSettings[userId].ContainsKey(name))
							UserSettings[userId].Add(name, setting?.Value ?? string.Empty);
					}
				}

				UserSettings[userId].TryGetValue(name, out settingValue);
			}

			if (string.IsNullOrEmpty(settingValue)) {
				if (!Settings.ContainsKey(name)) {
					var setting = DbContext.SiteSettings.Where(r => r.Name == name && string.IsNullOrEmpty(r.UserId)).FirstOrDefault();

					lock (Settings) {
						if (!Settings.ContainsKey(name))
							Settings.Add(name, setting?.Value ?? string.Empty);
					}
				}

				Settings.TryGetValue(name, out settingValue);
			}

			return settingValue;
		}

		public int GetInt(string name, string userId = "") {
			var setting = GetSetting(name, userId);

			if (string.IsNullOrEmpty(setting))
				return default(int);

			return Convert.ToInt32(setting);
		}

		public bool GetBool(string name, string userId = "") {
			var setting = GetSetting(name, userId);

			try {
				return Convert.ToBoolean(setting);
			}
			catch (FormatException) {
				return false;
			}
		}

		public async Task<List<ViewModels.IndexItem>> GetUserSettingsList(string userId) {
			var settingNames = typeof(Constants.Settings).GetConstants();
			var settingsRecords = await DbContext.SiteSettings.Where(record => string.IsNullOrEmpty(record.UserId) || record.UserId == userId).OrderByDescending(record => record.UserId).ToListAsync();

			var settingsList = new List<ViewModels.IndexItem>();

			foreach (var item in settingNames) {
				var existingRecord = settingsRecords.FirstOrDefault(record => record.Name == item);

				if (!existingRecord?.AdminOnly ?? false) {
					settingsList.Add(new ViewModels.IndexItem {
						Key = item,
						Value = existingRecord?.Value ?? string.Empty,
					});
				}
			}

			return settingsList;
		}

		public void UpdateUserSettings(InputModels.UpdateAccountInput input) {
			var existingRecords = DbContext.SiteSettings.Where(s => s.UserId == input.Id).ToList();

			if (existingRecords.Any())
				DbContext.RemoveRange(existingRecords);

			foreach (var settingInput in input.Settings) {
				if (string.IsNullOrEmpty(settingInput.Value))
					continue;

				var siteSetting = DbContext.SiteSettings.FirstOrDefault(s => !s.AdminOnly && s.Name == settingInput.Key && string.IsNullOrEmpty(s.UserId));

				if (siteSetting != null) {
					var record = new DataModels.SiteSetting {
						UserId = input.Id,
						Name = siteSetting.Name,
						Value = settingInput.Value,
						AdminOnly = siteSetting.AdminOnly
					};

					DbContext.SiteSettings.Add(record);
				}
			}

			DbContext.SaveChanges();
		}

		public ServiceModels.ServiceResponse UpdateSiteSettings(InputModels.EditSettingsInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			foreach (var settingInput in input.Settings) {
				var existingRecords = DbContext.SiteSettings.Where(s => s.Name == settingInput.Key && string.IsNullOrEmpty(s.UserId)).ToList();

				if (existingRecords.Any())
					DbContext.RemoveRange(existingRecords);

				if (string.IsNullOrEmpty(settingInput.Value))
					continue;

				var record = new DataModels.SiteSetting {
					Name = settingInput.Key,
					Value = settingInput.Value,
					AdminOnly = settingInput.AdminOnly
				};

				DbContext.SiteSettings.Add(record);
			}

			DbContext.SaveChanges();

			serviceResponse.Message = $"Site settings were updated.";
			return serviceResponse;
		}
	}
}