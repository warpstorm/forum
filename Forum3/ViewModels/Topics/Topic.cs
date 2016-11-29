using System.Collections.Generic;
using Forum3.Interfaces.Messages;
using Forum3.ViewModels.Messages;

namespace Forum3.ViewModels.Topics {
	public class Topic {
		public int Id { get; internal set; }
		public TopicHeader TopicHeader { get; internal set; }
		public List<Message> Messages { get; internal set; }
		public bool CanManage { get; internal set; }
		public bool CanInvite { get; internal set; }
		public int TotalPages { get; internal set; }
		public int CurrentPage { get; internal set; }
		public bool IsAuthenticated { get; internal set; }
		public IMessageInput ReplyInput { get; set; }
	}
}
