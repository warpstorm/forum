using System;
using System.Threading.Tasks;

using DataModels = Forum3.Models.DataModels;
using ServiceModels = Forum3.Models.ServiceModels;
using ViewModels = Forum3.Models.ViewModels.Profile;

namespace Forum3.Services {
	public class ProfileService {
		DataModels.ApplicationDbContext DbContext { get; }
		ServiceModels.ContextUser ContextUser { get; }

		public ProfileService(
			DataModels.ApplicationDbContext dbContext,
			ContextUserFactory contextUserFactory
		) {
			DbContext = dbContext;
			ContextUser = contextUserFactory.GetContextUser();
		}

		public async Task<ViewModels.DetailsPage> DetailsPage(string id) {
			if (string.IsNullOrEmpty(id))
				id = ContextUser.ApplicationUser.Id;

			var userRecord = await DbContext.Users.FindAsync(id);

			if (userRecord == null)
				throw new Exception($"No record found with the id {id}");

			// TODO check access rights i.e trim email

			var viewModel = new ViewModels.DetailsPage {
				Id = userRecord.Email,
				DisplayName = userRecord.DisplayName,
				Email = userRecord.Email,
			};

			return viewModel;
		}
	}
}