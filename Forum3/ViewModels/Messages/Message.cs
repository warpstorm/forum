using System;

namespace Forum3.ViewModels.Messages {
	public class Message {
		public string Body { get; internal set; }
		public bool CanEdit { get; internal set; }
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
	}
}
