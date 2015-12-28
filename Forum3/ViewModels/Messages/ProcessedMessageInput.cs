using System.Collections.Generic;

namespace Forum3.ViewModels.Messages {
	public class ProcessedMessageInput
    {
		public string OriginalBody { get; set; }
		public string DisplayBody { get; set; }
		public string ShortPreview { get; set; }
		public string LongPreview { get; set; }
		public string Boards { get; set; }
		public List<string> MentionedUsers { get; set; }
	}
}
