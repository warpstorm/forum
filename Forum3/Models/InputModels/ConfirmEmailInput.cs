using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class ConfirmEmailInput {
		[Required]
		public string UserId { get; set; }

		[Required]
		public string Code { get; set; }
	}
}