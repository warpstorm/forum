using Forum.Models.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models.ControllerModels.Topics {
	public class CreateTopicInput {
		[Required]
		public string Body { get; set; }
		public List<int> SelectedBoards { get; set; } = new List<int>();
		public ECreateTopicSaveAction Action { get; set; }
		public DateTime? Start { get; set; }
		public DateTime? End { get; set; }
		public bool AllDay { get; set; }
	}
}