using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics {
	public class CreateTopicForm {
		public string Body { get; set; }
		public List<int> SelectedBoards { get; set; } = new List<int>();

		[HiddenInput]
		public DateTime? Start { get; set; }

		[HiddenInput]
		public DateTime? End { get; set; }

		[HiddenInput]
		public bool AllDay { get; set; }
	}
}