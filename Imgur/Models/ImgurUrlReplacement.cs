using Forum.Contracts;

namespace Forum.ExternalClients.Imgur.Models {
	internal class ImgurUrlReplacement : IUrlReplacement {
		public string ReplacementText { get; set; }
		public string Card { get; set; }
	}
}
