using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics {
	public class CreateTopicForm {
		public string Body { get; set; }
		public List<int> SelectedBoards { get; set; } = new List<int>();

		[HiddenInput]
		public string Start { get; set; }

		[HiddenInput]
		public string End { get; set; }

		[HiddenInput]
		public bool AllDay { get; set; }
	}
}