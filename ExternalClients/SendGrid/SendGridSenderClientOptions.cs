namespace Forum.ExternalClients.SendGrid {
	class SendGridSenderClientOptions {
		public string SendGridUser { get; set; }
		public string SendGridKey { get; set; }
		public string FromAddress { get; set; }
		public string FromName { get; set; }
	}
}