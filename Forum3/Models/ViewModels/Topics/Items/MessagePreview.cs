using System;

namespace Forum.Models.ViewModels.Topics.Items {
	public class MessagePreview {
		public int Id { get; set; }

		/// <summary>
		/// Used by the merge action
		/// </summary>
		public int SourceId { get; set; }

		public string ShortPreview { get; set; }
		public int Views { get; set; }
		public int Replies { get; set; }
		public int Pages { get; set; }
		public int Unread { get; set; }
		public bool Pinned { get; set; }
		public bool Popular { get; set; }

		public int LastReplyId { get; set; }
		public string LastReplyById { get; set; }
		public string LastReplyByName { get; set; }
		public string LastReplyPosted { get; set; }
		public string LastReplyPreview { get; set; }
		public DateTime LastReplyPostedDT { get; set; }
	}
}