using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.DataModels {
	[Table("SiteSettings")]
	public class SiteSetting {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
		public string UserId { get; set; }
	}
}