using System;

namespace Forum.Models.ViewModels.Topics {
	public class EventPreview {
		public int TopicId { get; set; }
		public string Title { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public bool AllDay { get; set; }
	}
}
