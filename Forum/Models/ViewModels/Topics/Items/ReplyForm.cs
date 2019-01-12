using Forum.Interfaces.Models.ViewModels;

namespace Forum.Models.ViewModels.Topics.Items {
	public class ReplyForm : IMessageViewModel {
		public string Id { get; set; }
		public string BoardId { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = nameof(Controllers.Topics.TopicReply);
		public string FormController { get; } = nameof(Controllers.Topics);
		public string ElementId { get; set; }
	}
}