using System.Collections.Generic;
using System.Linq;

namespace Forum.Models.ServiceModels {
	public class ServiceResponse {
		public string Message { get; set; }
		public string RedirectPath { get; set; }
		public bool Success => !ErrorCollection.Any();
		public Dictionary<string, string> Errors => new Dictionary<string, string>(ErrorCollection);

		Dictionary<string, string> ErrorCollection { get; } = new Dictionary<string, string>();

		public void Error(string message) => Error(string.Empty, message);
		public void Error(string key, string message) => ErrorCollection[key] = message;
	}
}