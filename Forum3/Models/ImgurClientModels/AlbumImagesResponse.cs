using Newtonsoft.Json;
using System.Collections.Generic;

namespace Forum3.Models.ImgurClientModels {
	public class AlbumImagesResponse {
		[JsonProperty("data")] public List<Image> Data { get; set; }
		[JsonProperty("success")] public bool Success { get; set; }
		[JsonProperty("status")] public int Status { get; set; }
	}
}
