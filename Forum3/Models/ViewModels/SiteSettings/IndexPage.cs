using System.Collections.Generic;

namespace Forum3.Models.ViewModels.SiteSettings {
	public class IndexPage {
		public List<KeyValuePair<string, string>> Settings { get; } = new List<KeyValuePair<string, string>>();
	}
}