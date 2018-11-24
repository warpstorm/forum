using System;

namespace Forum.Models.DataModels {
	public class Quote {
		public int Id { get; set; }
		public int MessageId { get; set; }
		public string PostedById { get; set; }
		public DateTime TimePosted { get; set; }
		public string OriginalBody { get; set; }
		public string DisplayBody { get; set; }
		public string SubmittedById { get; set; }
		public DateTime SubmittedTime { get; set; }
		public bool Approved { get; set; }
	}
}
