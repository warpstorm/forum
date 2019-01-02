using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Forum.Plugins.Recaptcha {
	public class Recaptcha3ValidatorResponse {
		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("score")]
		public float Score { get; set; }

		[JsonProperty("action")]
		public string Action { get; set; }

		[JsonProperty("challenge_ts")]
		public DateTime TimeStamp { get; set; }

		[JsonProperty("hostname")]
		public string Hostname { get; set; }
		
		[JsonProperty("error-codes")]
		public List<string> ErrorCodes { get; set; }
	}
}