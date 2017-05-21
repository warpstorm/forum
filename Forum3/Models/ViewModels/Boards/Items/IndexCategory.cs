using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Boards.Items {
	public class IndexCategory {
		public int Id { get; set; }
		public string Name { get; set; }
		public int DisplayOrder { get; set; }

		public List<IndexBoard> Boards { get; } = new List<IndexBoard>();
	}
}