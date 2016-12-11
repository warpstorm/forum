using System.Collections.Generic;

namespace Forum3.Models.ServiceModels {
	public class ServiceResponse {
		public bool Success { get; set; }
		public List<string> ModelErrors { get; set; }
	}
}
