using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ViewModels.Roles {
	public class CreatePage {
		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }
	}
}