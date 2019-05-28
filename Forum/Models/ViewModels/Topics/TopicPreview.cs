using System;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics {
	public class TopicPreview {
		public int Id { get; set; }

		/// <summary>
		/// Used by the merge action
		/// </summary>
		public int SourceId { get; set; }

		public string FirstMessageShortPreview { get; set; }
		public int ViewCount { get; set; }
		public int ReplyCount { get; set; }
		public int Pages { get; set; }
		public int Unread { get; set; }
		public bool Pinned { get; set; }
		public bool Popular { get; set; }
		public bool Event { get; set; }

		public int FirstMessageId { get; set; }
		public DateTime FirstMessageTimePosted { get; set; }
		public string FirstMessagePostedById { get; set; }
		public string FirstMessagePostedByName { get; set; }
		public bool FirstMessagePostedByBirthday { get; set; }

		public int LastMessageId { get; set; }
		public DateTime LastMessageTimePosted { get; set; }
		public string LastMessagePostedById { get; set; }
		public string LastMessagePostedByName { get; set; }
		public bool LastMessagePostedByBirthday { get; set; }

		public List<Boards.IndexBoard> Boards { get; set; }
	}
}