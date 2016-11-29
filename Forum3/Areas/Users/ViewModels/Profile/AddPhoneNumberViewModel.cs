using System.ComponentModel.DataAnnotations;

namespace Forum3.Areas.Users.ViewModels.Profile {
	public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }
}
