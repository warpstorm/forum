using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Forum3.Annotations;

namespace Forum3.Models.ViewModels.Smileys {
	public class CreatePage {
		public string Code { get; set; }

		[Required]
		[MaxFileSize(1024, ErrorMessage = "Maximum allowed file size is {0} KB")]
		public IFormFile File { get; set; }
	}
}