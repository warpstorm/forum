using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels {
    [Table("Participants")]
    public class Participant
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MessageId { get; set; }
    }
}