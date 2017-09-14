using System;
using Forum3.Interfaces.Models.ViewModels;

namespace Forum3.Models.ViewModels.Topics.Items {
	public class Message {
		public string Body { get; set; }
		public string Cards { get; set; }
		public string OriginalBody { get; set; }
		public bool CanThought { get; set; }
		public bool CanEdit { get; set; }
		public bool CanReply { get; set; }
		public bool CanDelete { get; set; }
		public int Id { get; set; }
		public int ParentId { get; set; }
		public int ReplyId { get; set; }
		public string PostedById { get; set; }
		public string PostedByName { get; set; }
		public string ReplyBody { get; set; }
		public string ReplyPostedBy { get; set; }
		public string ReplyPreview { get; set; }
		public string TimeEdited { get; set; }
		public string TimePosted { get; set; }
		public DateTime RecordTime { get; set; }
		public DateTime TimePostedDT { get; set; }
		public DateTime TimeEditedDT { get; set; }
		public IMessageViewModel ReplyForm { get; set; }
	}
}