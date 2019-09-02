using Newtonsoft.Json;

namespace Forum.ExternalClients.Imgur.Models {
	public class Response<T> {
		[JsonProperty("data")] public T Data { get; set; }
		[JsonProperty("success")] public bool Success { get; set; }
		[JsonProperty("status")] public int Status { get; set; }
	}
}
