using Newtonsoft.Json;

namespace Forum.Plugins.UrlReplacement.ImgurClientModels {
	public class Image {
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("description")] public string Description { get; set; }
		[JsonProperty("datetime")] public int DateTime { get; set; }
		[JsonProperty("type")] public string Type { get; set; }
		[JsonProperty("animated")] public bool Animated { get; set; }
		[JsonProperty("width")] public int Width { get; set; }
		[JsonProperty("height")] public int Height { get; set; }
		[JsonProperty("size")] public int Size { get; set; }
		[JsonProperty("views")] public int Views { get; set; }
		[JsonProperty("bandwidth")] public long Bandwidth { get; set; }
		[JsonProperty("deletehash")] public string DeleteHash { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("section")] public string Section { get; set; }
		[JsonProperty("link")] public string Link { get; set; }
		[JsonProperty("gifv")] public string Gifv { get; set; }
		[JsonProperty("mp4")] public string Mp4 { get; set; }
		[JsonProperty("mp4_size")] public int Mp4Size { get; set; }
		[JsonProperty("looping")] public bool Looping { get; set; }
		[JsonProperty("favorite")] public bool Favorite { get; set; }
		[JsonProperty("nsfw")] public bool? Nsfw { get; set; }
		[JsonProperty("vote")] public string Vote { get; set; }
		[JsonProperty("in_gallery")] public bool InGallery { get; set; }
	}
}
