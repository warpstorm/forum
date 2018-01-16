using Forum3.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	public class SiteSettingsService {
		DataModels.ApplicationDbContext DbContext { get; }
		SettingsRepository Settings { get; }

		public SiteSettingsService(
			DataModels.ApplicationDbContext dbContext,
			SettingsRepository settingsRepository
		) {
			DbContext = dbContext;
			Settings = settingsRepository;
		}

		public ViewModels.IndexPage IndexPage() {
			var viewModel = new ViewModels.IndexPage();

			var settingNames = typeof(Constants.Settings).GetConstants();

			foreach (var settingName in settingNames) {
				var settingValue = Settings.GetSetting(settingName);
				viewModel.Settings.Add(new KeyValuePair<string, string>(settingName, settingValue));
			}

			return viewModel;
		}

		public ServiceModels.ServiceResponse Edit(InputModels.EditSettingsInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			foreach (var settingInput in input.Settings) {
				var existingRecords = DbContext.SiteSettings.Where(s => s.Name == settingInput.Key && string.IsNullOrEmpty(s.UserId)).ToList();

				if (existingRecords.Any())
					DbContext.RemoveRange(existingRecords);

				if (string.IsNullOrEmpty(settingInput.Value))
					continue;

				var record = new DataModels.SiteSetting {
					Name = settingInput.Key,
					Value = settingInput.Value
				};

				DbContext.SiteSettings.Add(record);
			}

			DbContext.SaveChanges();

			serviceResponse.Message = $"The smiley was updated.";
			return serviceResponse;
		}
	}
}