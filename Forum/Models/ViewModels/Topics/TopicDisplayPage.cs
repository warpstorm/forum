using Forum.Models.ViewModels.Boards;
using System;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics {
	public class TopicDisplayPage {
		public int Id { get; set; }
		public int FirstMessageId { get; set; }
		public string Subject { get; set; }
		public bool IsAuthenticated { get; internal set; }
		public bool IsOwner { get; internal set; }
		public bool IsAdmin { get; set; }
		public bool IsBookmarked { get; set; }
		public bool IsPinned { get; set; }
		public bool ShowFavicons { get; set; }
		public int TotalPages { get; internal set; }
		public int ViewCount { get; set; }
		public int ReplyCount { get; set; }
		public DateTime? Start { get; set; }
		public DateTime? End { get; set; }
		public bool AllDay { get; set; }
		public int CurrentPage { get; internal set; }
		public IMessageFormViewModel ReplyForm { get; set; }

		public List<Messages.DisplayMessage> Messages { get; set; }
		public List<IndexCategory> Categories { get; set; }
		public List<IndexBoard> AssignedBoards { get; set; }
	}
}