using Forum.Models.ViewModels.Boards.Items;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Boards.Pages {
	public class IndexPage {
		public List<IndexCategory> Categories { get; set; }
		public Sidebar.Sidebar Sidebar { get; set; }
	}
}