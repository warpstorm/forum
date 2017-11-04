using Forum3.Models.ViewModels.Topics.Items;
using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Topics.Pages {
	public class TopicIndexPage {
		public int BoardId { get; set; }
		public string BoardName { get; set; }
		public int Skip { get; set; }
		public int Take { get; set; }
		public int TotalPages { get; internal set; }
		public int CurrentPage { get; internal set; }
		public List<MessagePreview> Topics { get; set; }
	}
}