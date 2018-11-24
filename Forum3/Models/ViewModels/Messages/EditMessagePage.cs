﻿using Forum.Interfaces.Models.ViewModels;

namespace Forum.Models.ViewModels.Messages {
	public class EditMessagePage : IMessageViewModel {
		public int Id { get; set; }
		public int? BoardId { get; set; }
		public string Body { get; set; }
		public string FormAction { get; } = nameof(Controllers.Messages.Edit);
		public string FormController { get; } = nameof(Controllers.Messages);
	}
}