using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.DataModels {
	[Table("Boards")]
	public class Board {
		public int Id { get; set; }
		public string Name { get; set; }
		public bool VettedOnly { get; set; }
		public int? ParentId { get; set; }
		public int? LastMessageId { get; set; }
		public int DisplayOrder { get; set; }

		public Board Parent { get; set; }
		public Message LastMessage { get; set; }
	}
}