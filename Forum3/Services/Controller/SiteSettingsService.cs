using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	public class SiteSettingsService {
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
	}
}