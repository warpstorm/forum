using Forum3.Helpers;
using System.Threading.Tasks;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
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
				viewModel.Settings.Add(settingName, settingValue);
			}

			return viewModel;
		}
	}
}