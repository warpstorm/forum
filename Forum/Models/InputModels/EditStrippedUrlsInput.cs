using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class EditStrippedUrlsInput {
		[MaxLength(200)]
		public string NewUrl { get; set; }

		[MaxLength(200)]
		public string NewRegex { get; set; }

		public EditStrippedUrlInput[] StrippedUrls { get; set; }
	}
}