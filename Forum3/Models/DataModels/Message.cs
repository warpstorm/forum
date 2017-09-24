using System;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class Message {
		public int Id { get; set; }
		public int LegacyId { get; set; }

		/// <summary>
		/// Original unmodified source for the post.
		/// </summary>
		[Required]
		[DataType(DataType.MultilineText)]
		public string OriginalBody { get; set; }

		/// <summary>
		/// The display version of the post, with HTML embedded and BBC replaced.
		/// </summary>
		[Required]
		[DataType(DataType.MultilineText)]
		public string DisplayBody { get; set; }

		/// <summary>
		/// Follow-on text that displays under the message body.
		/// </summary>
		[DataType(DataType.MultilineText)]
		public string Cards { get; set; }

		/// <summary>
		/// Longer preview without images or other embedded HTML. Useful for quotes.
		/// </summary>
		[Required]
		[DataType(DataType.MultilineText)]
		public string LongPreview { get; set; }

		/// <summary>
		/// Shorter preview without images or other embedded HTML. Useful for topic lists.
		/// </summary>
		[Required]
		public string ShortPreview { get; set; }

		public DateTime TimePosted { get; set; }
		public DateTime TimeEdited { get; set; }
		public DateTime LastReplyPosted { get; set; }

		[Required]
		public string PostedById { get; set; }

		[Required]
		public string EditedById { get; set; }

		[Required]
		public string LastReplyById { get; set; }

		public int ParentId { get; set; }
		public int ReplyId { get; set; }
		public int LastReplyId { get; set; }

		public int ViewCount { get; set; }
		public int ReplyCount { get; set; }

		public bool Processed { get; set; }

		public int LegacyParentId { get; set; }
		public int LegacyReplyId { get; set; }
		public int LegacyPostedById { get; set; }
		public int LegacyEditedById { get; set; }
		public int LegacyLastReplyById { get; set; }
	}
}
