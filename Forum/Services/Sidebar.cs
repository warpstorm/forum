using Forum.Services.Repositories;
using System.Threading.Tasks;

namespace Forum.Services {
	using ViewModels = Models.ViewModels;

	public class Sidebar {
		AccountRepository AccountRepository { get; }
		QuoteRepository QuoteRepository { get; }

		public Sidebar(
			AccountRepository accountRepository,
			QuoteRepository quoteRepository
		) {
			AccountRepository = accountRepository;
			QuoteRepository = quoteRepository;
		}

		public async Task<ViewModels.Sidebar.Sidebar> Generate() {
			var sidebar = new ViewModels.Sidebar.Sidebar {
				Quote = await QuoteRepository.Get(),
				OnlineUsers = await AccountRepository.GetOnlineList(),
			};

			return sidebar;
		}
	}
}
