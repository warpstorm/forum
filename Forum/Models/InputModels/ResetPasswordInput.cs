using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class ResetPasswordInput {
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[Required]
		[Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
		[DataType(DataType.Password)]
		public string ConfirmPassword { get; set; }

		[Required]
		public string Code { get; set; }
	}
}