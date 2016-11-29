using Forum3.Interfaces.Messages;

namespace Forum3.ViewModels.Messages {
	public class TopicReplyPost : IMessageInput
    {
		public int Id { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = "TopicReply";
	}
}