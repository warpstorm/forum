using System;

namespace Forum.Models.ControllerModels.Topics {
	public class CreateEventInput {
		public DateTime? Start { get; set; }
		public DateTime? End { get; set; }
		public bool AllDay { get; set; }
		public int TopicId { get; set; } = -1;
		public string Body { get; set; }
		public string SelectedBoards { get; set; }
	}
}