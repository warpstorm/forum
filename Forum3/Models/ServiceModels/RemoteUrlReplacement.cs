using System.Text.RegularExpressions;

namespace Forum3.Models.ServiceModels {
	public class RemoteUrlReplacement {
		public Regex Regex { get; set; }
		public string ReplacementText { get; set; }
		public string FollowOnText { get; set; }
	}
}
