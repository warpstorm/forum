using System;

namespace Forum3.Models.ViewModels.Topics.Items {
	public class MessagePreview {
		public int Id { get; set; }
		public string ShortPreview { get; set; }
		public int Views { get; set; }
		public int Replies { get; set; }
		public int Unread { get; set; }
		public bool Pinned { get; set; }
		public bool Popular { get; set; }

		public int LastReplyId { get; set; }
		public string LastReplyById { get; set; }
		public string LastReplyByName { get; set; }
		public string LastReplyPosted { get; set; }
		public DateTime LastReplyPostedDT { get; set; }
	}
}
