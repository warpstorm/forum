using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.DataModels {
    [Table("Participants")]
	public class Participant {
		public int Id { get; set; }
		public string UserId { get; set; }
		public int MessageId { get; set; }
	}
}