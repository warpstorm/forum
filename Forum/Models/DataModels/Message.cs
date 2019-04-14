using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.DataModels {
	public class Message {
		public int Id { get; set; }

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

		[Required]
		public string PostedById { get; set; }

		public int TopicId { get; set; }
		public int ReplyId { get; set; }

		public bool Deleted { get; set; }
	}
}
