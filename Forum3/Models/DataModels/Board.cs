using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.DataModels {
	public class Board {
		public int Id { get; set; }

		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		public bool VettedOnly { get; set; }
		public int? ParentId { get; set; }
		public int? LastMessageId { get; set; }
		public int DisplayOrder { get; set; }

		public Board Parent { get; set; }
		public Message LastMessage { get; set; }
	}
}