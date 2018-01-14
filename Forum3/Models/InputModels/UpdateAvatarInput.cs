using Forum3.Annotations;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.InputModels {
	public class UpdateAvatarInput {
		[Required]
		public string Id { get; set; }

		[Required]
		[MaxFileSize(256, ErrorMessage = "Maximum allowed file size is {0} KB")]
		public IFormFile NewAvatar { get; set; }
	}
}