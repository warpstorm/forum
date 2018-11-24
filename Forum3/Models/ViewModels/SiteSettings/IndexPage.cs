using System.Collections.Generic;

namespace Forum.Models.ViewModels.SiteSettings {
	public class IndexPage {
		public List<IndexItem> Settings { get; set; } = new List<IndexItem>();
	}
}