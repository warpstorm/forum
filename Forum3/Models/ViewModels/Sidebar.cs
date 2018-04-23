using System.Collections.Generic;

namespace Forum3.Models.ViewModels {
	public class Sidebar {
		public string[] Birthdays { get; set; }
		public List<Boards.Items.OnlineUser> OnlineUsers { get; set; }
		public List<Notifications.Items.IndexItem> Notifications { get; set; }
	}
}