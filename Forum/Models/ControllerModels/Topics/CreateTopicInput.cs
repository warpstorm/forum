using Forum.Models.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ControllerModels.Topics {
	public class CreateTopicInput {
		[Required]
		public string Body { get; set; }
		public List<int> SelectedBoards { get; set; } = new List<int>();
		public ECreateTopicSaveAction Action { get; set; }
		public string Start { get; set; }
		public string End { get; set; }
		public bool AllDay { get; set; }
	}
}