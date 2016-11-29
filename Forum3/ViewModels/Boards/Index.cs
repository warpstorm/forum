using System.Collections.Generic;

namespace Forum3.ViewModels.Boards {
	public class Index {
		public string[] Birthdays { get; set; }
		public List<Board> Boards { get; set; }
		public List<OnlineUser> OnlineUsers { get; set; }
	}
}
