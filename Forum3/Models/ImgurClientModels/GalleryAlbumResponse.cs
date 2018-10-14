using Newtonsoft.Json;

namespace Forum3.Models.ImgurClientModels {
	public class GalleryAlbumResponse {
		[JsonProperty("data")] public GalleryAlbum Data { get; set; }
		[JsonProperty("success")] public bool Success { get; set; }
		[JsonProperty("status")] public int Status { get; set; }
	}
}
