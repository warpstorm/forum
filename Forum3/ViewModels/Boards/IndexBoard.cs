using System.Collections.Generic;
using Forum3.ViewModels.Messages;

namespace Forum3.ViewModels.Boards {
	public class IndexBoard {
		public int Id { get; set; }
		public int? Parent { get; set; }
		public string Name { get; set; }
		public int DisplayOrder { get; set; }
		public bool VettedOnly { get; set; }
		public bool InviteOnly { get; set; }
		public bool Unread { get; set; }
		public bool Selected { get; set; }
		public List<IndexBoard> Children { get; set; }

		public MessagePreview LastMessage { get; set; }
	}
}