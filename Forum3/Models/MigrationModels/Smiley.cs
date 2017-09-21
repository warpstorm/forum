using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels {
    [Table("Smileys")]
    public class Smiley
    {
        public int Id { get; set; }
        public decimal? DisplayOrder { get; set; }
        public string Code { get; set; }
        public string Path { get; set; }
        public string Thought { get; set; }
    }
}