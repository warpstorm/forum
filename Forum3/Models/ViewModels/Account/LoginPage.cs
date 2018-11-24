using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ViewModels.Account {
	public class LoginPage {
		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		public bool RememberMe { get; set; }
	}
}