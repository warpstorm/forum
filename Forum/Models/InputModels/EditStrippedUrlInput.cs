using System.ComponentModel.DataAnnotations;

namespace Forum.Models.InputModels {
	public class EditStrippedUrlInput {
		[Required]
		[MaxLength(200)]
		public string Url { get; set; }

		[Required]
		[MaxLength(200)]
		public string RegexPattern { get; set; }
	}
}