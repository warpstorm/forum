using Forum.Enums;

namespace Forum.Models.ViewModels.Notifications.Items {
	public class IndexItem {
		public int Id { get; set; }
		public string TargetUser { get; set; }
		public string Text { get; set; }
		public string Time { get; set; }
		public ENotificationType Type { get; set; }
		public bool Recent { get; set; }
	}
}