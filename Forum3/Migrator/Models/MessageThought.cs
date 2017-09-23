using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
	[Table("MessageThoughts")]
	public class MessageThought
	{
		public int Id { get; set; }
        public int SmileyId { get; set; }
		public int MessageId { get; set; }
		public int UserId { get; set; }
	}
}