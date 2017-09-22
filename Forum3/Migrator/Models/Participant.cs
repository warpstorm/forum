using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
    [Table("Participants")]
    public class Participant
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MessageId { get; set; }
    }
}