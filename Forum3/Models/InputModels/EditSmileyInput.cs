using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class EditSmileyInput {
		[Required]
		public int Id { get; set; }

		[Required]
		public int Column { get; set; }

		[Required]
		public int Row { get; set; }

		[Required]
		[MinLength(2)]
		[MaxLength(16)]
		public string Code { get; set; }

		[MaxLength(200)]
		public string Thought { get; set; }
	}
}