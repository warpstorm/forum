using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class UpdateAccountInput {
		[Required]
		public string Id { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(64)]
		[RegularExpression(@"(^\s+.+|.+\s+$)")]
		public string DisplayName { get; set; }

		[Required]
		[EmailAddress]
		public string NewEmail { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(100)]
		public string Password { get; set; }

		[MinLength(3)]
		[MaxLength(100)]
		public string NewPassword { get; set; }

		[Range(1, 31)]
		public int BirthdayDay { get; set; }

		[Range(1, 12)]
		public int BirthdayMonth { get; set; }

		[Range(1900, 2100)]
		public int BirthdayYear { get; set; }

		public EditSettingInput[] Settings { get; set; }
	}
}