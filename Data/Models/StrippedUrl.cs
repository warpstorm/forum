using System.ComponentModel.DataAnnotations;

namespace Forum.Data.Models {
	public class StrippedUrl {
		[Key]
		public string Url { get; set; }
		public string RegexPattern { get; set; }
	}
}
