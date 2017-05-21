using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.ViewModels.Roles.Items {
	public class IndexRole {
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int NumberOfUsers { get; set; }
		public string CreatedBy { get; set; }
	}
}