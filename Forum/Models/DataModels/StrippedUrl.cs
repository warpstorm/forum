using System.ComponentModel.DataAnnotations;

namespace Forum.Models.DataModels {
	public class StrippedUrl {
		[Key]
		public string Url { get; set; }
		public string RegexPattern { get; set; }
	}
}
