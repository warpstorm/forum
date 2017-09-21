namespace Forum3.Models.MigrationModels {
    public class SiteSetting
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int UserId { get; set; }
    }
}