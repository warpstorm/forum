using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class CreateBoardInput {
		[Required]
		[StringLength(64, MinimumLength = 3)]
		public string Name { get; set; }

		[StringLength(512)]
		public string Description { get; set; }

		[StringLength(64)]
		public string Category { get; set; }

		[StringLength(64)]
		public string NewCategory { get; set; }
	}
}