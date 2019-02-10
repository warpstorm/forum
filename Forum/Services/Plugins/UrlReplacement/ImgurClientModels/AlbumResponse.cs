using Newtonsoft.Json;

namespace Forum.Services.Plugins.UrlReplacement.ImgurClientModels {
	public class AlbumResponse {
		[JsonProperty("data")] public Album Data { get; set; }
		[JsonProperty("success")] public bool Success { get; set; }
		[JsonProperty("status")] public int Status { get; set; }
	}
}
