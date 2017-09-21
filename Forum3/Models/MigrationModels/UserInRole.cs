using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels {
	[Table("webpages_UsersInRoles")]
	public class UserInRole
	{
		[Key, Column(Order = 0)]
		public int RoleId { get; set; }

		[Key, Column(Order = 1)]
		public int UserId { get; set; }
	}
}