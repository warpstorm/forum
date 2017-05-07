using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Forum3.Models.ViewModels.Boards.Pages {
	public class CreatePage {
		public string Name { get; set; }
		public string Parent { get; set; }
		public bool VettedOnly { get; set; }
		public List<SelectListItem> Parents { get; set; }
	}
}