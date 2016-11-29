using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.DataModels {
	[Table("MessageBoards")]
	public class MessageBoard {
		public int Id { get; set; }
		public int MessageId { get; set; }
		public int BoardId { get; set; }
		public string UserId { get; set; }

		public DateTime TimeAdded { get; set; }

		public Message Message { get; set; }
		public Board Board { get; set; }
	}
}