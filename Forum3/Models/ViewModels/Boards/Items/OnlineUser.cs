using System;

namespace Forum3.Models.ViewModels.Boards.Items {
	public class OnlineUser {
		public string Id { get; set; }
		public string Name { get; set; }
		public bool Online { get; set; }
		public string LastOnlineString { get; set; }
		public DateTime LastOnline { get; set; }
	}
}
