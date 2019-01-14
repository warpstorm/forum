using System;
using System.Collections.Generic;

namespace Forum.Models.DataModels {
	public class ActionLogItem {
		public int Id { get; set; }
		public string UserId { get; set; }
		public string Description { get; set; }
		public DateTime Timestamp { get; set; }
		public string Action { get; set; }
		public string Controller { get; set; }
		public IDictionary<string, object> Arguments { get; set; }
	}
}
