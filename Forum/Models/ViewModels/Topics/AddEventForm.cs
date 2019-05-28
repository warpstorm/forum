using Microsoft.AspNetCore.Mvc;

namespace Forum.Models.ViewModels.Topics {
	public class AddEventForm {
		public string Start { get; set; }
		public string End { get; set; }
		public bool AllDay { get; set; }

		[HiddenInput]
		public int TopicId { get; set; } = -1;

		[HiddenInput]
		public string Body { get; set; } = string.Empty;

		[HiddenInput]
		public string SelectedBoards { get; set; } = string.Empty;
	}
}