using Newtonsoft.Json;

namespace Forum.Services.Plugins.UrlReplacement.ImgurClientModels {
	public class Account {
		[JsonProperty("id")] public int Id { get; set; }
		[JsonProperty("url")] public string Url { get; set; }
		[JsonProperty("bio")] public string Bio { get; set; }
		[JsonProperty("avatar")] public string Avatar { get; set; }
		[JsonProperty("reputation")] public int Reputation { get; set; }
		[JsonProperty("reputation_name")] public string ReputationName { get; set; }
		[JsonProperty("created")] public int Created { get; set; }
	}
}
