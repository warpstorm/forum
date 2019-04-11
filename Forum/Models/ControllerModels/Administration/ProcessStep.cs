namespace Forum.Models.ControllerModels.Administration {
	public class ProcessStep {
		public int CurrentStep { get; set; } = -1;
		public int Take { get; set; }
		public int LastRecordId { get; set; }
	}
}
