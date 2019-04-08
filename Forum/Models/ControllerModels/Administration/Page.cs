namespace Forum.Models.ControllerModels.Administration {
	public class Page {
		public int CurrentPage { get; set; } = -1;
		public int Take { get; set; }
		public int LastRecordId { get; set; }
	}
}
