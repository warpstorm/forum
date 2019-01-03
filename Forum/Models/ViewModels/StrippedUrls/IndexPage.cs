using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ViewModels.StrippedUrls {
	public class IndexPage {
		[MaxLength(200)]
		public string NewUrl { get; set; }

		[MaxLength(200)]
		public string NewRegex { get; set; }

		public List<IndexItem> StrippedUrls { get; set; } = new List<IndexItem>();
	}
}