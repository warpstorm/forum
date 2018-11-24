using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ViewModels.Account {
	public class ForgotPasswordPage {
		[Required]
		[EmailAddress]
		public string Email { get; set; }
	}
}