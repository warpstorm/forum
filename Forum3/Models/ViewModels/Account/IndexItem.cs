using Forum3.Models.DataModels;

namespace Forum3.Models.ViewModels.Account {
	public class IndexItem {
		public ApplicationUser User { get; set; }
		public bool CanManage { get; set; }
		public string Registered { get; set; }
		public string LastOnline { get; set; }
	}
}