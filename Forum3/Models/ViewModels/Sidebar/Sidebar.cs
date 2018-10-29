using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Sidebar {
	public class Sidebar {
		public Quote Quote { get; set; }
		public string[] Birthdays { get; set; }
		public List<Profile.OnlineUser> OnlineUsers { get; set; }
		public List<Notifications.Items.IndexItem> Notifications { get; set; }
	}
}