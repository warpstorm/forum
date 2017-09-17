using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class ForgotPasswordInput {
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}
}