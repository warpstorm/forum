using System.Collections.Generic;

namespace Forum.Models.ServiceModels {
	public class BaseSetting {
		public string Key { get; set; }
		public string Display { get; set; }
		public string Description { get; set; }
		public List<string> Options { get; set; }
	}
}