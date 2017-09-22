using System;

namespace Forum3.Migrator.Models {
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? TargetUserId { get; set; }
        public int? MessageId { get; set; }

        public DateTime Time { get; set; }
        public bool Unread { get; set; }
        public OldNotificationType Type { get; set; }

		public virtual UserProfile TargetUser { get; set; }
		public virtual Message Message { get; set; }
	}

	public enum OldNotificationType {
		Reply,
		Quote,
		Thought,
		Invite,
		PrivateReply,
		Mention
	}
}