namespace Forum.Models.ViewModels.Topics.Pages {
	public class CreateTopicForm : IMessageFormViewModel {
		public string Id { get; set; }
		public string BoardId { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = nameof(Controllers.Topics.Create);
		public string FormController { get; } = nameof(Controllers.Topics);
		public string ElementId { get; set; }
	}
}