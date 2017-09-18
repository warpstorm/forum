using System.ComponentModel.DataAnnotations;
using Forum3.Annotations;
using Microsoft.AspNetCore.Http;

namespace Forum3.Models.ViewModels.Account {
	public class DetailsPage {
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

		[MinLength(3)]
		[MaxLength(100)]
		[DataType(DataType.Password)]
		public string NewPassword { get; set; }

		[MaxFileSize(256, ErrorMessage = "Maximum allowed file size is {0} KB")]
		public IFormFile NewAvatar { get; set; }
		public string AvatarPath { get; set; }
	}
}