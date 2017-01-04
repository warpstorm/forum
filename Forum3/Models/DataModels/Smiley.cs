using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class Smiley {
		public int Id { get; set; }
		public decimal? DisplayOrder { get; set; }

		[Required]
		public string Code { get; set; }

		[Required]
		public string Path { get; set; }
	}
}