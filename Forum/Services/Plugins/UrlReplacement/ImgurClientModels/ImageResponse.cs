using Newtonsoft.Json;

namespace Forum.Plugins.UrlReplacement.ImgurClientModels {
	public class ImageResponse {
		[JsonProperty("data")] public Image Data { get; set; }
		[JsonProperty("success")] public bool Success { get; set; }
		[JsonProperty("status")] public int Status { get; set; }
	}
}
