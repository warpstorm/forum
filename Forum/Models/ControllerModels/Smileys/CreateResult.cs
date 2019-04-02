using System.Collections.Generic;

namespace Forum.Models.ControllerModels.Smileys {
	public class CreateResult {
		public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
	}
}
