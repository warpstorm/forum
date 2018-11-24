using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.SiteSettings {
	public class IndexItem {
		public bool AdminOnly { get; set; }
		public string Key { get; set; }
		public string Display { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public List<SelectListItem> Options { get; set; }
	}
}