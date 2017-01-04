using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class MessageThought {
		public int Id { get; set; }

		[Required]
		public int SmileyId { get; set; }

		[Required]
		public int MessageId { get; set; }

		[Required]
		public string UserId { get; set; }

		public Smiley Smiley { get; set; }
		public Message Message { get; set; }
		public ApplicationUser User { get; set; }
	}
}