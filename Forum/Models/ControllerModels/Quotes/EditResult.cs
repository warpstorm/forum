using System.Collections.Generic;

namespace Forum.Models.ControllerModels.Quotes {
	public class EditResult {
		public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
	}
}
