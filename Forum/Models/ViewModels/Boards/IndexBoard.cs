using Forum.Models.ViewModels.Topics;

namespace Forum.Models.ViewModels.Boards {
	public class IndexBoard {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int DisplayOrder { get; set; }
		public bool Unread { get; set; }

		public TopicPreview RecentTopic { get; set; }
	}
}