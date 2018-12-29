using Forum.Models.ViewModels.Topics.Items;

namespace Forum.Models.ViewModels.Boards.Items {
	public class IndexBoard {
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public int DisplayOrder { get; set; }
		public bool Unread { get; set; }

		public MessagePreview LastMessage { get; set; }
	}
}