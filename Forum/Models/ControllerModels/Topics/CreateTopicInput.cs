using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ControllerModels.Topics {
	public class CreateTopicInput {
		[Required]
		public string Body { get; set; }
		public int? BoardId { get; set; }
		public List<int> SelectedBoards { get; set; } = new List<int>();
	}
}