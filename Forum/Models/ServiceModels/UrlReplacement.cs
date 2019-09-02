using Forum.Contracts;

namespace Forum.Models.ServiceModels {
	class UrlReplacement : IUrlReplacement {
		public string ReplacementText { get; set; }
		public string Card { get; set; }
	}
}
