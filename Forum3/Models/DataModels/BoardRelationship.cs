using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.DataModels {
	[Table("BoardRelationships")]
	public class BoardRelationship {
		public int Id { get; set; }
		public int ParentId { get; set; }
		public int ChildId { get; set; }

		public Board Parent { get; set; }
		public Board Child { get; set; }
	}
}