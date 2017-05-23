using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class EditRoleInput {
		[Required]
		public string Id { get; set; }

		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }
	}
}