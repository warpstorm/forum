using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class SiteSetting {
		public int Id { get; set; }

		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		[Required]
		[StringLength(64)]
		public string Value { get; set; }

		public string UserId { get; set; }
	}
}