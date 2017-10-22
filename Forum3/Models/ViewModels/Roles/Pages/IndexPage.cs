using Forum3.Models.ViewModels.Roles.Items;
using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Roles.Pages {
	public class IndexPage {
		public List<IndexRole> Roles { get; set; } = new List<IndexRole>();
	}
}