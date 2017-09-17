using System.Collections.Generic;
using System.Linq;

namespace Forum3.Models.ServiceModels {
	public class ServiceResponse {
		public string Message { get; set; }
		public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
		public string RedirectPath { get; set; }
		public bool Success => !Errors.Any();
	}
}