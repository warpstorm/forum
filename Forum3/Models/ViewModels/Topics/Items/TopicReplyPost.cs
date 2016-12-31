using Forum3.Interfaces.Models.ViewModels;

namespace Forum3.ViewModels.Topics.Items {
	public class TopicReplyPost : IMessageViewModel {
		public int Id { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = "TopicReply";
		public bool AllowCancel { get; } = false;
	}
}