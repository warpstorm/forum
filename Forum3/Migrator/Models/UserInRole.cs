using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Migrator.Models {
	[Table("webpages_UsersInRoles")]
	public class UserInRole
	{
		public int RoleId { get; set; }
		public int UserId { get; set; }
	}
}