namespace Forum.Models.ViewModels.Quotes {
	public class DisplayQuote {
		public int TopicId { get; set; }
		public int MessageId { get; set; }
		public string DisplayBody { get; set; }
		public string PostedBy { get; set; }
	}
}
