using Forum.Annotations;
using Forum.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ViewModels.Account {
	public class DetailsPage {
		public string Id { get; set; }

		[Required]
		public string DisplayName { get; set; }

		[Required]
		[EmailAddress]
		public string NewEmail { get; set; }

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

		public bool ShowBirthday { get; set; }

		public string BirthdayDay { get; set; }
		public string BirthdayMonth { get; set; }
		public string BirthdayYear { get; set; }

		public IEnumerable<SelectListItem> BirthdayMonths { get; set; }
		public IEnumerable<SelectListItem> BirthdayDays { get; set; }
		public IEnumerable<SelectListItem> BirthdayYears { get; set; }

		[MaxFileSize(256, ErrorMessage = "Maximum allowed file size is {0} KB")]
		public IFormFile NewAvatar { get; set; }
		public string AvatarPath { get; set; }

		public EFrontPage FrontPage { get; set; }
		public IEnumerable<SelectListItem> FrontPageOptions { get; set; }

		[Range(5, 50)]
		public int MessagesPerPage { get; set; }

		[Range(5, 50)]
		public int TopicsPerPage { get; set; }

		[Range(5, 100)]
		public int PopularityLimit { get; set; }

		public bool Poseys { get; set; }
		public bool ShowFavicons { get; set; }
	}
}
