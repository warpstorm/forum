using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class CreateRoleInput {
		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }
	}
}