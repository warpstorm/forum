namespace Forum.Models.ViewModels.Events {
	public class CreateForm {
		public string TopicId { get; } = string.Empty;
		public string FormAction { get; } = nameof(Controllers.Topics.Create);
		public string FormController { get; } = nameof(Controllers.Topics);

		public string Start { get; set; }
		public string End { get; set; }
		public bool AllDay { get; set; }
	}
}