using Forum3.Interfaces.Models.ViewModels;

namespace Forum3.Models.ViewModels.Messages {
	public class CreateTopicPage : IMessageViewModel {
		public int Id { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = nameof(Controllers.Messages.Create);
		public string FormController { get; } = nameof(Controllers.Messages);
		public string CancelPath { get; set; }
	}
}