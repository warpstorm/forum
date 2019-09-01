using Forum.Services.Repositories;
using System.Threading.Tasks;

namespace Forum.Services {
	using ViewModels = Models.ViewModels;

	public class Sidebar {
		QuoteRepository QuoteRepository { get; }

		public Sidebar(
			QuoteRepository quoteRepository
		) {
			QuoteRepository = quoteRepository;
		}

		public async Task<ViewModels.Sidebar.Sidebar> Generate() {
			var sidebar = new ViewModels.Sidebar.Sidebar {
				Quote = await QuoteRepository.Get()
			};

			return sidebar;
		}
	}
}
