using Forum.Models.ViewModels.Topics.Items;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics.Pages {
	public class TopicIndexPartialPage {
		public int CurrentPage { get; set; }
		public bool MorePages { get; set; }
		public List<MessagePreview> Topics { get; set; }
	}
}