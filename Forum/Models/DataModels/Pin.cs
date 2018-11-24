using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.DataModels {
	public class Pin {
		public int Id { get; set; }

		[Required]
		public int MessageId { get; set; }

		[Required]
		public string UserId { get; set; }

		public DateTime Time { get; set; }
	}
}