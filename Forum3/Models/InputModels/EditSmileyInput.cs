using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class EditSmileyInput {
		[Required]
		public int Id { get; set; }

		[Required]
		[MinLength(2)]
		[MaxLength(10)]
		public string Code { get; set; }
	}
}