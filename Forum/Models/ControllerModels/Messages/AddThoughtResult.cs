using System.Collections.Generic;

namespace Forum.Models.ControllerModels.Messages {
	public class AddThoughtResult {
		public int TopicId { get; set; }
		public int MessageId { get; set; }
		public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
	}
}
