using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.ViewModels.Account {
	public class DetailsPage {
		[Required]
		[EmailAddress]
		public string Id { get; set; }

		[Required]
		public string DisplayName { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		public bool EmailConfirmed { get; set; }

		[Required]
		[MinLength(3)]
		[MaxLength(100)]
		[DataType(DataType.Password)]
		public string Password { get; set; }
	}
}