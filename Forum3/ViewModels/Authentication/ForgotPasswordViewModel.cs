using System.ComponentModel.DataAnnotations;

namespace Forum3.ViewModels.Authentication {
	public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
