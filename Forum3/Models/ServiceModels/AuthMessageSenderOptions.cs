namespace Forum3.Models.ServiceModels {
	public class AuthMessageSenderOptions {
		public string SendGridUser { get; set; }
		public string SendGridKey { get; set; }
		public string FromAddress { get; set; }
		public string FromName { get; set; }
	}
}