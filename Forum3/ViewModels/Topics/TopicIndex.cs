using System.Collections.Generic;
using Forum3.ViewModels.Messages;

namespace Forum3.ViewModels.Topics {
	public class TopicIndex {
		public int Skip { get; set; }
		public int Take { get; set; }
		public bool MoreMessages { get; set; }
		public List<MessagePreview> Topics { get; set; }
	}
}
