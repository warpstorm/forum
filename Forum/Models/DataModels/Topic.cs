using System;

namespace Forum.Models.DataModels {
	public class Topic {
		public int Id { get; set; }
		public int ViewCount { get; set; }
		public int ReplyCount { get; set; }
		public bool Pinned { get; set; }
		public bool Deleted { get; set; }

		public int FirstMessageId { get; set; }
		public string FirstMessageShortPreview { get; set; }
		public DateTime FirstMessageTimePosted { get; set; }
		public string FirstMessagePostedById { get; set; }

		public int LastMessageId { get; set; }
		public string LastMessageShortPreview { get; set; }
		public DateTime LastMessageTimePosted { get; set; }
		public string LastMessagePostedById { get; set; }
	}
}
