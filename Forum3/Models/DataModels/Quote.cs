using System;

namespace Forum3.Models.DataModels {
	public class Quote {
		public int Id { get; set; }
		public int MessageId { get; set; }
		public string PostedById { get; set; }
		public string Body { get; set; }
		public string SubmittedById { get; set; }
		public DateTime SubmittedTime { get; set; }
	}
}
