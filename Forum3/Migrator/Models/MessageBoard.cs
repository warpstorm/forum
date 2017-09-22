using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
	[Table("MessageBoards")]
	public class MessageBoard
	{
		public MessageBoard()
		{
			TimeAdded = DateTime.Now;
		}

		public int Id { get; set; }
		public int MessageId { get; set; }
		public int BoardId { get; set; }
		public int UserId { get; set; }

		public DateTime TimeAdded { get; set; }

        public virtual Message Message { get; set; }
        public virtual Board Board { get; set; }
	}
}