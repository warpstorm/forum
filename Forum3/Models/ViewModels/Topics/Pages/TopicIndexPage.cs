using System.Collections.Generic;
using Forum3.Models.ViewModels.Topics.Items;

namespace Forum3.Models.ViewModels.Topics.Pages {
	public class TopicIndexPage {
		public int BoardId { get; set; }
		public int Skip { get; set; }
		public int Take { get; set; }
		public bool MoreMessages { get; set; }
		public List<MessagePreview> Topics { get; set; }
	}
}
