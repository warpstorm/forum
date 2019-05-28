namespace Forum.Models.ControllerModels.Topics {
	public class CreateEventInput {
		public string Start { get; set; }
		public string End { get; set; }
		public bool AllDay { get; set; }
		public int TopicId { get; set; } = -1;
		public string Body { get; set; }
		public string SelectedBoards { get; set; }
	}
}