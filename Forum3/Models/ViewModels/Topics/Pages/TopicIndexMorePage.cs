using Forum.Models.ViewModels.Topics.Items;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics.Pages {
	public class TopicIndexMorePage {
		public bool More { get; set; }
		public int Page { get; set; }
		public List<MessagePreview> Topics { get; set; }
	}
}