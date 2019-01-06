using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class RegisterInput {
		[Required]
		[MinLength(3)]
		[MaxLength(64)]
		[RegularExpression(@"(^[^\s]+.+[^\s]+$)", ErrorMessage = "The display name cannot have spaces before or after.")]
		public string DisplayName { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(100)]
		public string Password { get; set; }

		public bool ConfirmThirteen { get; set; }
	}
}