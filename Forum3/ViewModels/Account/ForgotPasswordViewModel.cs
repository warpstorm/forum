using System.ComponentModel.DataAnnotations;

namespace Forum3.ViewModels.Account {
	public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
