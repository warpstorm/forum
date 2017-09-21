using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Forum3.Models.DataModels {
	public class ApplicationUser : IdentityUser {
		[Required]
		[StringLength(64)]
		public string DisplayName { get; set; }

		public int LegacyId { get; set; }
		public string LegacyPassword { get; set; }

		public DateTime Birthday { get; set; } = new DateTime(1900, 1, 1);
		public DateTime Registered { get; set; } = new DateTime(1900, 1, 1);
		public DateTime LastOnline { get; set; } = new DateTime(1900, 1, 1);

		public string AvatarPath { get; set; }
	}
}