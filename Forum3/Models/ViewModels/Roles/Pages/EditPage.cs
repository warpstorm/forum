using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.ViewModels.Roles.Pages {
	public class EditPage {
		public string Id { get; set; }

		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }

		public string CreatedBy { get; set; }
		public string Created { get; set; }

		public string ModifiedBy { get; set; }
		public string Modified { get; set; }

		public int NumberOfUsers { get; set; }
	}
}