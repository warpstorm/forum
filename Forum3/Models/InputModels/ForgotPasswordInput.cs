using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class ForgotPasswordInput {
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}
}