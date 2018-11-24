using Forum.Models.DataModels;

namespace Forum.Models.ViewModels.Account {
	public class IndexItem {
		public bool CanManage { get; set; }

		public string Id { get; set; }
		public string DisplayName { get; set; }
		public string Email { get; set; }
		public string Registered { get; set; }
		public string LastOnline { get; set; }
	}
}