using System;

namespace Forum.Models.DataModels {
	public class Event {
		public int Id { get; set; }
		public int TopicId { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public bool AllDay { get; set; }
	}
}
