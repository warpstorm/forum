using Forum3.Models.ViewModels.Boards.Items;
using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Boards.Pages {
	public class IndexPage {
		public string[] Birthdays { get; set; }
		public List<IndexCategory> Categories { get; set; }
		public List<OnlineUser> OnlineUsers { get; set; }
		public List<Notifications.Items.IndexItem> Notifications { get; set; }
	}
}