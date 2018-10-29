namespace Forum3.Models.ViewModels.Quotes {
	public class EditQuote {
		public int Id { get; set; }
		public int MessageId { get; set; }
		public string OriginalBody { get; set; }
		public string DisplayBody { get; set; }
		public string PostedBy { get; set; }
		public string PostedTime { get; set; }
		public string SubmittedBy { get; set; }
		public string SubmittedTime { get; set; }
		public bool Approved { get; set; }
	}
}
