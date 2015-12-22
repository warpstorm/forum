using System;

namespace Forum3.ViewModels.Message {
	public class Topic
    {
		public int Id { get; set; }
		public string Subject { get; set; }
		public int Views { get; set; }
		public int Replies { get; set; }

		public int LastReplyId { get; set; }
		public string LastReplyById { get; set; }
		public string LastChildByName { get; set; }
		public string LastChildTimePosted { get; set; }
		public DateTime LastReplyPostedDT { get; set; }
	}
}
