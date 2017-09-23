using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
    [Table("InviteOnlyTopicUsers")]
    public class InviteOnlyTopicUsers
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
    }
}