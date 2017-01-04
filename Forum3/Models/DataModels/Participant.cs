using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class Participant {
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; }

		[Required]
		public int MessageId { get; set; }
	}
}