using System;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.DataModels {
	public class TopicBoard {
		public int Id { get; set; }

		[Required]
		public int MessageId { get; set; }

		[Required]
		public int TopicId { get; set; }

		[Required]
		public int BoardId { get; set; }

		[Required]
		public string UserId { get; set; }

		public DateTime TimeAdded { get; set; }
	}
}