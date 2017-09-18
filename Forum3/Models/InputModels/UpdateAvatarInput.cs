using System.ComponentModel.DataAnnotations;
using Forum3.Annotations;
using Microsoft.AspNetCore.Http;

namespace Forum3.Models.InputModels {
	public class UpdateAvatarInput {
		[Required]
		public string Id { get; set; }

		[Required]
		public string DisplayName { get; set; }

		[Required]
		[MaxFileSize(256, ErrorMessage = "Maximum allowed file size is {0} KB")]
		public IFormFile NewAvatar { get; set; }
	}
}