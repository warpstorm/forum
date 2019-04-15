using System.Collections.Generic;

namespace Forum.Models.ViewModels.Sidebar {
	public class Sidebar {
		public Quotes.DisplayQuote Quote { get; set; }
		public List<Account.OnlineUser> OnlineUsers { get; set; }
		public List<Notifications.IndexItem> Notifications { get; set; }
	}
}