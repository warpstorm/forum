using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Forum3.Models.DataModels {
	public class Category {
		public int Id { get; set; }

		[Required]
		[StringLength(64)]
		public string Name { get; set; }

		public int DisplayOrder { get; set; }

		public List<Board> Boards { get; set; }
	}
}