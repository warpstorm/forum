using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Forum3.Models.DataModels {
	public class ApplicationUser : IdentityUser {
		[Required]
		[StringLength(64)]
		public string DisplayName { get; set; }

		public DateTime Birthday { get; set; }
		public DateTime Registered { get; set; }
		public DateTime LastOnline { get; set; }
	}
}