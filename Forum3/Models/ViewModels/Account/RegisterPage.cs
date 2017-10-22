using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.ViewModels.Account {
	public class RegisterPage {
		[Required]
		[StringLength(64, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
		public string DisplayName { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[EmailAddress]
		[Compare(nameof(Email), ErrorMessage = "The email and confirmation email do not match.")]
		public string ConfirmEmail { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }

		public string BirthdayDay { get; set; }
		public string BirthdayMonth { get; set; }
		public string BirthdayYear { get; set; }

		public IEnumerable<SelectListItem> BirthdayMonths { get; set; }
		public IEnumerable<SelectListItem> BirthdayDays { get; set; }
		public IEnumerable<SelectListItem> BirthdayYears { get; set; }
	}
}