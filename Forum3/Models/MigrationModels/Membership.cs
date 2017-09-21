using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Forum3.Models.MigrationModels
{
	[Table("webpages_Membership")]
	public class Membership
    {
		[Key]
		public int UserId { get; set; }
		public DateTime CreateDate { get; set; }
		public string Password { get; set; }
	}
}