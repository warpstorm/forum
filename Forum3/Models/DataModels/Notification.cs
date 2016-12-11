using System;
using System.ComponentModel.DataAnnotations.Schema;
using Forum3.Enums;

namespace Forum3.DataModels {
	[Table("Notifications")]
	public class Notification {
		public int Id { get; set; }
		public int UserId { get; set; }
		public string TargetUserId { get; set; }
		public int? MessageId { get; set; }

		public DateTime Time { get; set; }
		public bool Unread { get; set; }
		public ENotificationType Type { get; set; }

		public ApplicationUser TargetUser { get; set; }
		public Message Message { get; set; }
	}
}