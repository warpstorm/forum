using Forum3.Models.DataModels;
using System.Collections.Generic;

namespace Forum3.Contexts {
	public class UserContext {
		public bool IsAuthenticated { get; set; }
		public bool IsAdmin { get; set; }
		public List<string> Roles { get; set; }
		public List<ViewLog> ViewLogs { get; set; }

		public ApplicationUser ApplicationUser { get; set; }
	}
}