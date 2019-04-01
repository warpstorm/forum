namespace Forum.Models.ViewModels {
	public class MultiStep {
		public string ActionName { get; set; }
		public string ActionNote { get; set; }
		public string Action { get; set; }
		public int Page { get; set; }
		public int TotalPages { get; set; }
		public int TotalRecords { get; set; }
		public int Take { get; set; }
	}
}
