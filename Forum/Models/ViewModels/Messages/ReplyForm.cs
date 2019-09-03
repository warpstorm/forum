namespace Forum.Models.ViewModels.Messages {
	public class ReplyForm : IMessageFormViewModel {
		public string Id { get; set; }
		public string TopicId { get; set; }
		public string BoardId { get; } = string.Empty;
		public string Body { get; set; }
		public string FormAction { get; set; } = nameof(Controllers.Messages.Reply);
		public string FormController { get; } = nameof(Controllers.Messages);
		public string ElementId { get; set; }
		public DisplayMessage ReplyMessage { get; set; }
	}
}