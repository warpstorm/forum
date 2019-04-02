using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ControllerModels.Messages {
	public class CreateReplyInput {
		public int Id { get; set; }

		[Required]
		public string Body { get; set; }

		[Required]
		[Range(0, int.MaxValue)]
		public int TopicId { get; set; }
	}
}