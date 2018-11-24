namespace Forum.Models.ViewModels.Messages {
	public class MigrateMessagePage {
		public int Id { get; set; }
		public int Page { get; set; }
		public int TotalPages { get; set; }
		public string RedirectPath { get; set; }
	}
}