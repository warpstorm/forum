using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class Role {
		[Required]
		[MaxLength(64)]
		public string Name { get; set; }

		[Required]
		[MaxLength(512)]
		public string Description { get; set; }
	}
}