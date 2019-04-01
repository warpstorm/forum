using Forum.Models.ViewModels.Boards.Items;
using Forum.Models.ViewModels.Topics.Items;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics.Pages {
	public class TopicDisplayPage {
		public int Id { get; internal set; }
		public string Subject { get; internal set; }
		public List<Messages.DisplayMessage> Messages { get; internal set; }
		public List<IndexCategory>	Categories { get; set; }
		public List<IndexBoard> AssignedBoards { get; set; }
		public bool IsAuthenticated { get; internal set; }
		public bool IsOwner { get; internal set; }
		public bool IsAdmin { get; set; }
		public bool IsBookmarked { get; set; }
		public bool IsPinned { get; set; }
		public bool ShowFavicons { get; set; }
		public int TotalPages { get; internal set; }
		public int ViewCount { get; set; }
		public int ReplyCount { get; set; }
		public int CurrentPage { get; internal set; }
		public IMessageFormViewModel ReplyForm { get; set; }
		public string RedirectPath { get; set; }
	}
}