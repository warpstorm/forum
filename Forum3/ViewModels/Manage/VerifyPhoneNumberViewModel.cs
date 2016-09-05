using System.ComponentModel.DataAnnotations;

namespace Forum3.ViewModels.Manage {
	public class VerifyPhoneNumberViewModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }
}
