using Microsoft.AspNetCore.Mvc;
using System;

namespace Forum.Models.ViewModels.Topics {
	public class EditEventForm {
		public string FormAction { get; set; }
		public string FormController { get; set; }

		public DateTime? Start { get; set; }
		public DateTime? End { get; set; }
		public bool AllDay { get; set; }

		[HiddenInput]
		public int TopicId { get; set; } = -1;

		[HiddenInput]
		public string Body { get; set; } = string.Empty;

		[HiddenInput]
		public string SelectedBoards { get; set; } = string.Empty;
	}
}