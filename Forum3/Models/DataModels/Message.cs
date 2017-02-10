﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.DataModels {
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
		[StringLength(64)]
		public string PostedByName { get; set; }

		public ApplicationUser PostedBy { get; set; }

		[Required]
		public string EditedById { get; set; }

		[Required]
		[StringLength(64)]
		public string EditedByName { get; set; }

		public ApplicationUser EditedBy { get; set; }

		[Required]
		public string LastReplyById { get; set; }

		[Required]
		[StringLength(64)]
		public string LastReplyByName { get; set; }
		public ApplicationUser LastReplyBy { get; set; }

		public int ParentId { get; set; }
		public int ReplyId { get; set; }
		public int LastReplyId { get; set; }

		public int Views { get; set; }
		public int Replies { get; set; }

		public List<MessageThought> Thoughts { get; set; }
	}
}