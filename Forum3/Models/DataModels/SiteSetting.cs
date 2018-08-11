using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class SiteSetting {
		public int Id { get; set; }

		[Required]
		public string Name { get; set; }

		[Required]
		public string Value { get; set; }

		public bool AdminOnly { get; set; }

		public string UserId { get; set; }
	}
}