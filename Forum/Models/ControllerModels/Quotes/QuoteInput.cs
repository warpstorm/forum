namespace Forum.Models.ControllerModels.Quotes {
	public class QuoteInput {
		public int Id { get; set; }
		public string OriginalBody { get; set; }
		public bool Approved { get; set; }
	}
}
