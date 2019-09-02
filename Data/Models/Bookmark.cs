using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Data.Models {
	public class Bookmark {
		public int Id { get; set; }

		[Required]
		public int TopicId { get; set; }

		[Required]
		public string UserId { get; set; }

		public DateTime Time { get; set; }
	}
}