using Forum3.Interfaces.Models.ViewModels;

namespace Forum3.ViewModels.Topics.Items {
	public class DirectReplyPost : IMessageViewModel {
		public int Id { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = "DirectReply";
		public bool AllowCancel { get; } = true;
	}
}