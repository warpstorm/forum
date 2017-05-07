using System.Collections.Generic;

namespace Forum3.Models.ServiceModels {
	public class ServiceResponse {
		public string Message { get; set; }
		public Dictionary<string, string> ModelErrors { get; set; } = new Dictionary<string, string>();
		public string RedirectPath { get; set; }
	}
}