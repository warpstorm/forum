using System.Collections.Generic;

namespace Forum3.ViewModels.Topics {
	public class Topic {
		public int Id { get; internal set; }
		public TopicHeader TopicHeader { get; internal set; }
		public List<Messages.Message> Messages { get; internal set; }
		public bool CanManage { get; internal set; }
		public bool CanInvite { get; internal set; }
		public int TotalPages { get; internal set; }
		public int CurrentPage { get; internal set; }
		public bool IsAuthenticated { get; internal set; }
	}
}
