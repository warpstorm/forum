using Newtonsoft.Json;
using System.Collections.Generic;

namespace Forum.ExternalClients.Imgur.Models {
	public class GalleryAlbum {
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("datetime")] public int DateTime { get; set; }
		[JsonProperty("account_url")] public string AccountUrl { get; set; }
		[JsonProperty("account_id")] public int? AccountId { get; set; }
		[JsonProperty("privacy")] public string Privacy { get; set; }
		[JsonProperty("layout")] public string Layout { get; set; }
		[JsonProperty("link")] public string Link { get; set; }
		[JsonProperty("is_album")] public bool IsAlbum { get; set; }
		[JsonProperty("nsfw")] public bool? Nsfw { get; set; }
		[JsonProperty("images_count")] public int ImagesCount { get; set; }
		[JsonProperty("images")] public List<Image> Images { get; set; }
	}
}
