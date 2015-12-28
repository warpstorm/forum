using System.Collections.Generic;

namespace Forum3.ViewModels.Topics {
	public class Topic {
		public string StartedById { get; internal set; }
		public int TopicId { get; internal set; }
		public string Subject { get; internal set; }
		public List<Messages.Message> Messages { get; internal set; }
		public int Views { get; internal set; }
		public bool CanManage { get; internal set; }
		public bool CanInvite { get; internal set; }
	}
}
