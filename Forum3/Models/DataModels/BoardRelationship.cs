using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.DataModels {
	public class BoardRelationship {
		public int Id { get; set; }

		[Required]
		public int ParentId { get; set; }

		[Required]
		public int ChildId { get; set; }

		public Board Parent { get; set; }
		public Board Child { get; set; }
	}
}