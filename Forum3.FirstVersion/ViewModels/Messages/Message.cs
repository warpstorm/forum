using System;

namespace Forum3.ViewModels.Messages {
	public class Message {
		public string Body { get; internal set; }
		public string OriginalBody { get; internal set; }
		public bool CanThought { get; internal set; }
		public bool CanEdit { get; internal set; }
		public bool CanReply { get; internal set; }
		public bool CanDelete { get; internal set; }
		public int Id { get; internal set; }
		public int ParentId { get; internal set; }
		public int ReplyId { get; internal set; }
		public string PostedById { get; internal set; }
		public string PostedByName { get; internal set; }
		public string ReplyBody { get; internal set; }
		public string ReplyPostedBy { get; internal set; }
		public string ReplyPreview { get; internal set; }
		public string TimeEdited { get; internal set; }
		public string TimePosted { get; internal set; }
		public DateTime RecordTime { get; internal set; }
		public DateTime TimePostedDT { get; internal set; }
		public DateTime TimeEditedDT { get; internal set; }
		public Input EditInput { get; set; }
		public Input ReplyInput { get; set; }
	}
}
