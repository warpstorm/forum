using Newtonsoft.Json;
using System.Collections.Generic;

namespace Forum.ExternalClients.Imgur.Models {
	public class Album {
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("datetime")] public int DateTime { get; set; }
		[JsonProperty("cover")] public string Cover { get; set; }
		[JsonProperty("cover_width")] public int CoverWidth { get; set; }
		[JsonProperty("cover_height")] public int CoverHeight { get; set; }
		[JsonProperty("account_url")] public string AccountUrl { get; set; }
		[JsonProperty("account_id")] public int? AccountId { get; set; }
		[JsonProperty("privacy")] public string Privacy { get; set; }
		[JsonProperty("layout")] public string Layout { get; set; }
		[JsonProperty("views")] public int Views { get; set; }
		[JsonProperty("link")] public string Link { get; set; }
		[JsonProperty("favorite")] public bool Favorite { get; set; }
		[JsonProperty("nsfw")] public bool? Nsfw { get; set; }
		[JsonProperty("section")] public string Section { get; set; }
		[JsonProperty("order")] public int Order { get; set; }
		[JsonProperty("deletehash")] public string DeleteHash { get; set; }
		[JsonProperty("images_count")] public int ImagesCount { get; set; }
		[JsonProperty("images")] public List<Image> Images { get; set; }
		[JsonProperty("in_gallery")] public bool InGallery { get; set; }
	}
}
