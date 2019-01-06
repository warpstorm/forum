using Forum.Enums;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class UpdateAccountInput {
		[Required]
		public string Id { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(64)]
		[RegularExpression(@"(^[^\s]+.+[^\s]+$)", ErrorMessage = "The display name cannot have spaces before or after.")]
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

		public bool ShowBirthday { get; set; }

		[Range(1, 31)]
		public int BirthdayDay { get; set; }

		[Range(1, 12)]
		public int BirthdayMonth { get; set; }

		public EFrontPage FrontPage { get; set; }

		[Range(5, 50)]
		public int MessagesPerPage { get; set; }

		[Range(5, 50)]
		public int TopicsPerPage { get; set; }

		[Range(5, 100)]
		public int PopularityLimit { get; set; }

		public bool Poseys { get; set; }
		public bool ShowFavicons { get; set; }
	}
}