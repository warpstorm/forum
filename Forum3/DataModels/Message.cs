using System;
using System.ComponentModel.DataAnnotations;

namespace Forum3.DataModels {
	public class Message
    {
		public int Id { get; set; }

		/// <summary>
		/// Original unmodified source for the post.
		/// </summary>
		[DataType(DataType.MultilineText)]
		public string OriginalBody { get; set; }

		/// <summary>
		/// The display version of the post, with HTML embedded and BBC replaced.
		/// </summary>
		[DataType(DataType.MultilineText)]
		public string DisplayBody { get; set; }

		/// <summary>
		/// Longer preview without images or other embedded HTML. Useful for quotes.
		/// </summary>
		[DataType(DataType.MultilineText)]
		public string LongPreview { get; set; }

		/// <summary>
		/// Shorter preview without images or other embedded HTML. Useful for topic lists.
		/// </summary>
		public string ShortPreview { get; set; }

		public DateTime TimePosted { get; set; }
		public DateTime TimeEdited { get; set; }
		public DateTime LastReplyPosted { get; set; }

		public string PostedById { get; set; }
		public string PostedByName { get; internal set; }
		public string EditedById { get; set; }
		public string EditedByName { get; internal set; }
		public string LastReplyById { get; set; }
		public string LastReplyByName { get; internal set; }

		public int? ParentId { get; set; }
		public int? ReplyId { get; set; }
		public int? LastReplyId { get; set; }

		public int Views { get; set; }
		public int Replies { get; set; }
	}
}
