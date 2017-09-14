using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using DataModels = Forum3.Models.DataModels;
using ViewModels = Forum3.Models.ViewModels.Smileys;

namespace Forum3.Services {
	public class SmileyService {
		DataModels.ApplicationDbContext DbContext { get; }

		public SmileyService(
			DataModels.ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public async Task<ViewModels.IndexPage> IndexPage() {
			var smileysQuery = from smiley in DbContext.Smileys
							   orderby smiley.SortOrder
							   select smiley;

			var smileys = await smileysQuery.ToListAsync();

			var viewModel = new ViewModels.IndexPage();

			foreach (var smiley in smileys) {
				var sortColumn = smiley.SortOrder / 100;
				var sortRow = smiley.SortOrder % 100;

				viewModel.Smileys.Add(new ViewModels.Smiley {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return viewModel;
		}
	}
}