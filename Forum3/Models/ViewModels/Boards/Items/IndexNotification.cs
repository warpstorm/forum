using Forum3.Enums;

namespace Forum3.Models.ViewModels.Boards.Items {
	public class IndexNotification {
		public int Id { get; set; }
		public string TargetUser { get; set; }
		public string Text { get; set; }
		public string Time { get; set; }
		public ENotificationType Type { get; set; }
	}
}