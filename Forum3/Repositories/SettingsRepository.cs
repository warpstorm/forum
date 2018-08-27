using Forum3.Contexts;
using Forum3.Errors;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	public class SettingsRepository : Repository<DataModels.SiteSetting> {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();
		Dictionary<string, Dictionary<string, string>> UserSettings { get; } = new Dictionary<string, Dictionary<string, string>>();

		public SettingsRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			ILogger<SettingsRepository> log
		) : base(log) {
			DbContext = dbContext;
			UserContext = userContext;
		}

		public int AvatarSize(bool forceGlobal = false) {
			var setting = GetInt("AvatarSize", forceGlobal);

			if (setting == 0)
				setting = 100;

			return setting;
		}

		public DateTime HistoryTimeLimit(bool forceGlobal = false) {
			var setting = GetInt("HistoryTimeLimit", forceGlobal);

			if (setting == 0)
				setting = -14;

			return DateTime.Now.AddDays(setting);
		}

		public int MessagesPerPage(bool forceGlobal = false) {
			var setting = GetInt("MessagesPerPage", forceGlobal);

			if (setting == 0)
				setting = 15;

			return setting;
		}

		public int OnlineTimeLimit(bool forceGlobal = false) {
			var setting = GetInt("OnlineTimeLimit", forceGlobal);

			if (setting == 0)
				setting = 5;

			return setting;
		}

		public int PopularityLimit(bool forceGlobal = false) {
			var setting = GetInt("PopularityLimit", forceGlobal);

			if (setting == 0)
				setting = 25;

			return setting;
		}

		public int TopicsPerPage(bool forceGlobal = false) {
			var setting = GetInt("TopicsPerPage", forceGlobal);

			if (setting == 0)
				setting = 15;

			return setting;
		}

		public bool ShowFavicons(bool forceGlobal = false) =>  GetBool("ShowFavicons", forceGlobal);

		public List<string> PoseyUsers(bool forceGlobal = false) {
			var value = GetSetting("PoseyUsers", forceGlobal);
			return value.Split("|").ToList();
		}

		public List<string> StrippedUrls(bool forceGlobal = false) {
			var value = GetSetting("StrippedUrls", forceGlobal);
			return value.Split("|").ToList();
		}

		public string FrontPage(bool forceGlobal = false) {
			var setting = GetSetting("FrontPage", forceGlobal);

			if (string.IsNullOrEmpty(setting))
				setting = "Board List";

			return setting;
		}

		public string GetSetting(string name, bool forceGlobal) {
			var settingValue = string.Empty;

			if (!forceGlobal) {
				var userId = UserContext.ApplicationUser.Id;

				if (!UserSettings.ContainsKey(userId)) {
					lock (UserSettings) {
						UserSettings.TryAdd(userId, new Dictionary<string, string>());
					}
				}

				if (!UserSettings[userId].ContainsKey(name)) {
					var setting = Records.FirstOrDefault(r => r.Name == name && r.UserId == userId);

					lock (UserSettings[userId]) {
						if (!UserSettings[userId].ContainsKey(name))
							UserSettings[userId].Add(name, setting?.Value ?? string.Empty);
					}
				}

				UserSettings[userId].TryGetValue(name, out settingValue);
			}

			if (string.IsNullOrEmpty(settingValue)) {
				if (!Settings.ContainsKey(name)) {
					var setting = Records.FirstOrDefault(r => r.Name == name && string.IsNullOrEmpty(r.UserId));

					lock (Settings) {
						if (!Settings.ContainsKey(name))
							Settings.Add(name, setting?.Value ?? string.Empty);
					}
				}

				Settings.TryGetValue(name, out settingValue);
			}

			return settingValue;
		}

		public int GetInt(string name, bool forceGlobal) {
			var setting = GetSetting(name, forceGlobal);

			if (string.IsNullOrEmpty(setting))
				return default(int);

			return Convert.ToInt32(setting);
		}

		public bool GetBool(string name, bool forceGlobal) {
			var setting = GetSetting(name, forceGlobal);

			try {
				return Convert.ToBoolean(setting);
			}
			catch (FormatException) {
				return false;
			}
		}

		public List<ViewModels.IndexItem> GetUserSettingsList(string userId) {
			var settings = new BaseSettings();

			var settingsRecords = Records.Where(record => string.IsNullOrEmpty(record.UserId) || record.UserId == userId).OrderByDescending(record => record.UserId).ToList();

			var settingsList = new List<ViewModels.IndexItem>();

			foreach (var item in settings) {
				var existingRecord = settingsRecords.FirstOrDefault(record => record.Name == item.Key);

				if (!existingRecord?.AdminOnly ?? false) {
					var options = new List<SelectListItem>();

					var value = existingRecord?.Value ?? string.Empty;

					if (item.Options != null) {
						foreach (var option in item.Options) {
							options.Add(new SelectListItem {
								Text = option,
								Value = option,
								Selected = option == value
							});
						}
					}

					settingsList.Add(new ViewModels.IndexItem {
						Key = item.Key,
						Display = item.Display,
						Description = item.Description,
						Options = options,
						Value = value,
					});
				}
			}

			return settingsList;
		}

		public void UpdateUserSettings(InputModels.UpdateAccountInput input) {
			var existingRecords = Records.Where(s => s.UserId == input.Id).ToList();

			if (existingRecords.Any())
				DbContext.RemoveRange(existingRecords);

			foreach (var settingInput in input.Settings) {
				if (string.IsNullOrEmpty(settingInput.Value))
					continue;

				var siteSetting = Records.FirstOrDefault(s => !s.AdminOnly && s.Name == settingInput.Key && string.IsNullOrEmpty(s.UserId));

				if (siteSetting != null) {
					var baseSetting = BaseSettings.Get(siteSetting.Name);

					if (baseSetting.Options != null && !baseSetting.Options.Contains(settingInput.Value))
						throw new HttpBadRequestError();

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
				var existingRecords = Records.Where(s => s.Name == settingInput.Key && string.IsNullOrEmpty(s.UserId)).ToList();

				if (existingRecords.Any())
					DbContext.RemoveRange(existingRecords);

				if (string.IsNullOrEmpty(settingInput.Value))
					continue;

				var baseSetting = BaseSettings.Get(settingInput.Key);

				if (baseSetting.Options != null && !baseSetting.Options.Contains(settingInput.Value))
					throw new HttpBadRequestError();

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

		protected override List<DataModels.SiteSetting> GetRecords() => DbContext.SiteSettings.ToList();
	}
}