namespace Forum3.Models.ViewModels {
	public class Error {
		public string RequestId { get; set; }

		public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
	}
}