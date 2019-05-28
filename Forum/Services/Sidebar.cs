using Forum.Services.Repositories;
using System.Threading.Tasks;

namespace Forum.Services {
	using ViewModels = Models.ViewModels;

	public class Sidebar {
		AccountRepository AccountRepository { get; }
		NotificationRepository NotificationRepository { get; }
		QuoteRepository QuoteRepository { get; }
		TopicRepository TopicRepository { get; }

		public Sidebar(
			AccountRepository accountRepository,
			NotificationRepository notificationRepository,
			QuoteRepository quoteRepository,
			TopicRepository topicRepository
		) {
			AccountRepository = accountRepository;
			NotificationRepository = notificationRepository;
			QuoteRepository = quoteRepository;
			TopicRepository = topicRepository;
		}

		public async Task<ViewModels.Sidebar.Sidebar> Generate() {
			var sidebar = new ViewModels.Sidebar.Sidebar {
				Quote = await QuoteRepository.Get(),
				OnlineUsers = await AccountRepository.GetOnlineList(),
				Notifications = await NotificationRepository.Index(),
				Events = await TopicRepository.GetUpcomingEvents()
			};

			return sidebar;
		}
	}
}
