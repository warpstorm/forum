namespace Forum.Models.ServiceModels {
	public class EmailSenderOptions {
		public string SendGridUser { get; set; }
		public string SendGridKey { get; set; }
		public string FromAddress { get; set; }
		public string FromName { get; set; }
	}
}