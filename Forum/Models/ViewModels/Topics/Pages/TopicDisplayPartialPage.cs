using Forum.Models.ViewModels.Topics.Items;
using System.Collections.Generic;

namespace Forum.Models.ViewModels.Topics.Pages {
	public class TopicDisplayPartialPage {
		public long Latest { get; set; }
		public List<Message> Messages { get; set; }
	}
}
