using Forum.Contracts;

namespace Forum.ExternalClients.YouTube {
	class YouTubeUrlReplacement : IUrlReplacement {
		public string ReplacementText { get; set; }
		public string Card { get; set; }
	}
}