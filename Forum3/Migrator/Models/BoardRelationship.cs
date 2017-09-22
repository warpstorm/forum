using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
	[Table("BoardRelationships")]
	public class BoardRelationship
	{
		public int Id { get; set; }
		public int ParentId { get; set; }
		public int ChildId { get; set; }

	    public virtual Board Parent { get; set; }
	    public virtual Board Child { get; set; }
	}
}