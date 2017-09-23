using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models
{
	[Table("Boards")]
	public class Board
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public bool VettedOnly { get; set; }
	    public bool InviteOnly { get; set; }
		public int? ParentId { get; set; }
	    public int? LastMessageId { get; set; }
	    public int DisplayOrder { get; set; }
	}
}