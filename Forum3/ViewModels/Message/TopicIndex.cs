using System.Collections.Generic;

namespace Forum3.ViewModels.Message {
	public class TopicIndex
    {
		public int Skip { get; set; }
		public int Take { get; set; }
		public bool MoreMessages { get; set; }
		public List<Topic> Topics { get { return _topics ?? (_topics = new List<Topic>()); } }
		private List<Topic> _topics;
	}
}
