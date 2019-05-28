using Forum.Models.Annotations;
using System;

namespace Forum.Models.ControllerModels.Topics {
	public class EditEventInput {
		public DateTime? Start { get; set; }

		[MustBeAfter(nameof(Start))]
		public DateTime? End { get; set; }

		public bool AllDay { get; set; }

		public int TopicId { get; set; } = -1;
		public string Body { get; set; }
		public string SelectedBoards { get; set; }
	}
}
