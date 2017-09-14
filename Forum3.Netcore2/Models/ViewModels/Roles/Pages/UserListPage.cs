using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Forum3.Models.ViewModels.Roles.Items;

namespace Forum3.Models.ViewModels.Roles.Pages {
	public class UserListPage {
		public string Id { get; set; }

		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		public List<UserListItem> ExistingUsers { get; set; }
		public List<UserListItem> AvailableUsers { get; set; }
	}
}