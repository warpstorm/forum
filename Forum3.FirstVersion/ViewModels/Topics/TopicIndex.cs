using System.Collections.Generic;

namespace Forum3.ViewModels.Topics {
	public class TopicIndex
    {
		public int Skip { get; set; }
		public int Take { get; set; }
		public bool MoreMessages { get; set; }
		public List<TopicPreview> Topics { get; set; }
	}
}
