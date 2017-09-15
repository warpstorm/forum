using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Forum3.Annotations;

namespace Forum3.Models.ViewModels.Smileys {
	public class CreatePage {
		[Required]
		[MinLength(2)]
		[MaxLength(10)]
		public string Code { get; set; }

		[MaxLength(200)]
		public string Thought { get; set; }

		[Required]
		[MaxFileSize(1024, ErrorMessage = "Maximum allowed file size is {0} KB")]
		public IFormFile File { get; set; }
	}
}