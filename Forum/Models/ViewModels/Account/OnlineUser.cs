using System;

namespace Forum.Models.ViewModels.Account {
	public class OnlineUser {
		public string Id { get; set; }
		public string Name { get; set; }
		public DateTime LastOnline { get; set; }
		public bool IsOnline { get; set; }
		public int LastActionLogItemId { get; set; }
		public string Action { get; set; }
	}
}
