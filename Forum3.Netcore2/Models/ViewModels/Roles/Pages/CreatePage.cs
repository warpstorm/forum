using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.ViewModels.Roles.Pages {
	public class CreatePage {
		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }
	}
}