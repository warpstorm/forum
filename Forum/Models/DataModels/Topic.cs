using System;

namespace Forum.Models.DataModels {
	public class Topic {
		public int Id { get; set; }
		public string Subject { get; set; }
		public DateTime TimePosted { get; set; }
		public string PostedById { get; set; }
		public int ViewCount { get; set; }
		public int ReplyCount { get; set; }
		public bool Pinned { get; set; }
		public bool Deleted { get; set; }

		public int FirstMessageId { get; set; }
		public int LastMessageId { get; set; }
	}
}
