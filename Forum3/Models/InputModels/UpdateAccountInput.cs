using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class UpdateAccountInput {
		[Required]
		public string Id { get; set; }

		[Required]
		public string DisplayName { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(100)]
		public string Password { get; set; }

		[MinLength(3)]
		[MaxLength(100)]
		public string NewPassword { get; set; }
	}
}