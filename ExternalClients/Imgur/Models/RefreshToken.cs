using Newtonsoft.Json;

namespace Forum.ExternalClients.Imgur.Models {
	public class RefreshToken {
		[JsonProperty("access_token")] public string AccessToken { get; set; }
		[JsonProperty("expires_in")] public int ExpiresIn { get; set; }
		[JsonProperty("account_username")] public string UserName { get; set; }
	}
}
