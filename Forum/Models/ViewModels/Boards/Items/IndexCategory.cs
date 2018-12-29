using System.Collections.Generic;

namespace Forum.Models.ViewModels.Boards.Items {
	public class IndexCategory {
		public string Id { get; set; }
		public string Name { get; set; }
		public int DisplayOrder { get; set; }

		public List<IndexBoard> Boards { get; } = new List<IndexBoard>();
	}
}