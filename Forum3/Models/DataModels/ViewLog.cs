using Forum3.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class ViewLog {
		public int Id { get; set; }

		[Required]
		public string UserId { get; set; }
		public int? TargetId { get; set; }
		public EViewLogTargetType TargetType { get; set; }
		public DateTime LogTime { get; set; }
	}
}