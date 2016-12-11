using System;
using System.ComponentModel.DataAnnotations.Schema;
using Forum3.Enums;

namespace Forum3.DataModels {
	[Table("ViewLogs")]
	public class ViewLog {
		public int Id { get; set; }
		public string UserId { get; set; }
		public int? TargetId { get; set; }
		public EViewLogTargetType TargetType { get; set; }
		public DateTime LogTime { get; set; }
	}
}