using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels {
	[Table("ViewLogs")]
	public class ViewLog
	{
		public ViewLog()
		{
			LogTime = DateTime.Now;
		}

		public int Id { get; set; }
		public int UserId { get; set; }
		public int? TargetId { get; set; }
		public OldViewLogTargetType TargetType { get; set; }
		public DateTime LogTime { get; set; }
	}

	public enum OldViewLogTargetType {
		User,               // mark all topics read for the entire site
		Message,            // mark single message / topic as read
		BoardMessages,      // mark a whole board as read
		Board               // view a board
	}
}