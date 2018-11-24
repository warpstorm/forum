using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class ConfirmEmailInput {
		[Required]
		public string UserId { get; set; }

		[Required]
		public string Code { get; set; }
	}
}