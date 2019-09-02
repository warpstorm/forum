using System.ComponentModel.DataAnnotations;

namespace Forum.Data.Models {
	public class Category {
		public int Id { get; set; }

		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		public int DisplayOrder { get; set; }
	}
}