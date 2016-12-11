using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.DataModels {
	[Table("MessageThoughts")]
	public class MessageThought {
		public int Id { get; set; }

		public int SmileyId { get; set; }
		public int MessageId { get; set; }
		public string UserId { get; set; }

		public Smiley Smiley { get; set; }
		public Message Message { get; set; }
		public ApplicationUser User { get; set; }
	}
}