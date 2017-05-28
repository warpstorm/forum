using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class Board {
		public int Id { get; set; }

		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		public int CategoryId { get; set; }

		public bool VettedOnly { get; set; }
		public int? LastMessageId { get; set; }
		public int DisplayOrder { get; set; }

		public Message LastMessage { get; set; }
		public Category Category { get; set; }
	}
}