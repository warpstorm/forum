using System;

namespace Forum.Models.ViewModels.Profile {
	public class OnlineUser {
		public string Id { get; set; }
		public string Name { get; set; }
		public bool Online { get; set; }
		public string LastOnlineString { get; set; }
		public DateTime LastOnline { get; set; }
	}
}