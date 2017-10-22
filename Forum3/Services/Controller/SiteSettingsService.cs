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

		public async Task<ViewModels.IndexPage> IndexPage() {
			var viewModel = new ViewModels.IndexPage();

			var settingNames = typeof(Constants.Settings).GetConstants();

			foreach (var settingName in settingNames) {
				var settingValue = await Settings.GetSetting(settingName);
				viewModel.Settings.Add(new KeyValuePair<string, string>(settingName, settingValue));
			}

			return viewModel;
		}

		public async Task<ServiceModels.ServiceResponse> Edit(InputModels.EditSettingsInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			foreach (var settingInput in input.Settings) {
				var existingRecords = await DbContext.SiteSettings.Where(s => s.Name == settingInput.Key && string.IsNullOrEmpty(s.UserId)).ToListAsync();

				if (existingRecords.Any())
					DbContext.RemoveRange(existingRecords);

				if (string.IsNullOrEmpty(settingInput.Value))
					continue;

				var record = new DataModels.SiteSetting {
					Name = settingInput.Key,
					Value = settingInput.Value
				};

				await DbContext.SiteSettings.AddAsync(record);
			}

			await DbContext.SaveChangesAsync();

			serviceResponse.Message = $"The smiley was updated.";
			return serviceResponse;
		}
	}
}