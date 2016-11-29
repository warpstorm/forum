using System.ComponentModel.DataAnnotations;

namespace Forum3.Areas.Users.ViewModels.Authentication {
	public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
