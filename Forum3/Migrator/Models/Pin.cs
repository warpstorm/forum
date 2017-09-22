using System;

namespace Forum3.Migrator.Models {
    public class Pin
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime Time { get; set; }
    }
}