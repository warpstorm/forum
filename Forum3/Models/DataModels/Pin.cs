using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.DataModels {
	[Table("Pins")]
	public class Pin {
		public int Id { get; set; }
		public int MessageId { get; set; }
		public string UserId { get; set; }
		public DateTime Time { get; set; }
	}
}