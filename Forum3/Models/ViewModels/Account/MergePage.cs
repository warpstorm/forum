using System.Collections.Generic;

namespace Forum.Models.ViewModels.Account {
	public class MergePage {
		public string SourceId { get; set; }

		public List<IndexItem> IndexItems { get; set; } = new List<IndexItem>();
	}
}