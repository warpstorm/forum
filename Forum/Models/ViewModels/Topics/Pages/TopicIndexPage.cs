using Forum.Models.ViewModels.Topics.Items;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics.Pages {
	public class TopicIndexPage {
		/// <summary>
		/// Used in topic merging
		/// </summary>
		public int SourceId { get; set; }

		public int BoardId { get; set; }
		public int CurrentPage { get; set; }
		public int UnreadFilter { get; set; }
		public string BoardName { get; set; }
		public bool MorePages { get; set; }

		public List<TopicPreview> Topics { get; set; }
	}
}