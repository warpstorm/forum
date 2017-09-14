using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Forum3.Data;
using Forum3.Models.ViewModels.Smileys;

namespace Forum3.Services {
	public class SmileyService {
		ApplicationDbContext DbContext { get; }

		public SmileyService(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public async Task<IndexPage> IndexPage() {
			var smileysQuery = from smiley in DbContext.Smileys
							   orderby smiley.SortOrder
							   select smiley;

			var smileys = await smileysQuery.ToListAsync();

			var viewModel = new IndexPage();

			foreach (var smiley in smileys) {
				var sortColumn = smiley.SortOrder / 100;
				var sortRow = smiley.SortOrder % 100;

				viewModel.Smileys.Add(new Smiley {
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