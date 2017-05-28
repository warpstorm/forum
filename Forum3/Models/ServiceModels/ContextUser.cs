using Forum3.Models.DataModels;

namespace Forum3.Models.ServiceModels {
	public class ContextUser {
		public bool IsAuthenticated { get; set; }
		public bool IsAdmin { get; set; }

		public ApplicationUser ApplicationUser { get; set; }
	}
}