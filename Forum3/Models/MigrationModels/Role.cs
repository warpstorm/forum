using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels {
	[Table("webpages_Roles")]
	public class Role
	{
		[Key]
		[Column("RoleId")]
		public int Id { get; set; }

		[Column("RoleName")]
		[MaxLength(256)]
		[Required]
        [DisplayName("Role Name")]
		public string Name { get; set; }
	}
}