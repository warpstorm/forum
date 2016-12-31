using System.Collections.Generic;
using Forum3.ViewModels.Boards.Items;

namespace Forum3.ViewModels.Boards.Pages {
	public class IndexPage {
		public string[] Birthdays { get; set; }
		public List<IndexBoardSummary> Boards { get; set; }
		public List<OnlineUser> OnlineUsers { get; set; }
	}
}