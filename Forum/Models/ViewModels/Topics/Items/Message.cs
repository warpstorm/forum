using Forum.Interfaces.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics.Items {
	public class Message {
		public string Body { get; set; }
		public string Cards { get; set; }
		public string OriginalBody { get; set; }
		public bool CanThought { get; set; }
		public bool CanEdit { get; set; }
		public bool CanReply { get; set; }
		public bool CanQuote { get; set; }
		public bool CanDelete { get; set; }
		public bool Processed { get; set; }
		public bool Poseys { get; set; }
		public bool Birthday { get; set; }
		public string Id { get; set; }
		public int ParentId { get; set; }
		public int ReplyId { get; set; }
		public string PostedById { get; set; }
		public string PostedByName { get; set; }
		public string PostedByAvatarPath { get; set; }
		public string ReplyBody { get; set; }
		public string ReplyPostedBy { get; set; }
		public string ReplyPreview { get; set; }
		public DateTime RecordTime { get; set; }
		public DateTime TimePosted { get; set; }
		public DateTime TimeEdited { get; set; }
		public IMessageViewModel ReplyForm { get; set; }
		public List<MessageThought> Thoughts { get; set; }
	}
}