using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Forum.Services.Plugins.Recaptcha {
	public class Recaptcha2ValidatorResponse {
		[JsonProperty("hostname")]
		public string Hostname { get; set; }

		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("error-codes")]
		public List<string> ErrorCodes { get; set; }

		[JsonProperty("challenge_ts")]
		public DateTime TimeStamp { get; set; }
	}
}