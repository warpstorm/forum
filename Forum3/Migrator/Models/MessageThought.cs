using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
	[Table("MessageThoughts")]
	public class MessageThought
	{
		public int Id { get; set; }
		
        public int SmileyId { get; set; }
		public int MessageId { get; set; }
		public int UserId { get; set; }

        public virtual Smiley Smiley { get; set; }
        public virtual Message Message { get; set; }
        public virtual UserProfile User { get; set; }
	}
}