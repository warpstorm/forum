using Forum.Models.Options;
using System;

namespace Forum.Models.ViewModels.Notifications {
	public class IndexItem {
		public int Id { get; set; }
		public string TargetUserId { get; set; }
		public string TargetUser { get; set; }
		public string Text { get; set; }
		public DateTime Time { get; set; }
		public ENotificationType Type { get; set; }
		public bool Recent { get; set; }
	}
}