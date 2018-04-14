using System;
using System.Collections.Generic;

namespace Forum3.Models.ServiceModels {
	public class BaseSetting {
		public string Key { get; set; }
		public string Display { get; set; }
		public string Description { get; set; }
		public List<string> Options { get; set; }
	}
}