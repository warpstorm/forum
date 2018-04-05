using Forum3.Processes.Boards;
using Forum3.Processes.Users;
using Forum3.Services.Controller;

namespace Forum3.ViewModelProviders.Boards {
	using PageViewModels = Models.ViewModels.Boards.Pages;

	public class IndexPage {
		ListBirthdays ListBirthdays { get; }
		ListOnlineUsers ListOnlineUsers { get; }
		ListCategories ListCategories { get; }
		NotificationService NotificationService { get; }

		public IndexPage(
			ListBirthdays listBirthdays,
			ListOnlineUsers listOnlineUsers,
			ListCategories listCategories,
			NotificationService notificationService
        ) {
			ListBirthdays = listBirthdays;
			ListOnlineUsers = listOnlineUsers;
			ListCategories = listCategories;
			NotificationService = notificationService;
		}

		public PageViewModels.IndexPage Generate() {
            var birthdays = ListBirthdays.Execute();
            var onlineUsers = ListOnlineUsers.Execute();
            var notifications = NotificationService.GetNotifications();

            var viewModel = new PageViewModels.IndexPage {
                Birthdays = birthdays.ToArray(),
                Categories = ListCategories.Execute(),
                OnlineUsers = onlineUsers,
                Notifications = notifications
            };

            return viewModel;
        }
    }
}