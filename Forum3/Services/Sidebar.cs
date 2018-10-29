using Forum3.Repositories;

namespace Forum3.Services {
	using ViewModels = Models.ViewModels;

	public class Sidebar {
		AccountRepository AccountRepository { get; }
		NotificationRepository NotificationRepository { get; }

		public Sidebar(
			AccountRepository accountRepository,
			NotificationRepository notificationRepository
		) {
			AccountRepository = accountRepository;
			NotificationRepository = notificationRepository;
		}

		public ViewModels.Sidebar Generate() {
			var sidebar = new ViewModels.Sidebar {
				Birthdays = AccountRepository.GetBirthdaysList().ToArray(),
				OnlineUsers = AccountRepository.GetOnlineList(),
				Notifications = NotificationRepository.Index()
			};

			return sidebar;
		}
	}
}
