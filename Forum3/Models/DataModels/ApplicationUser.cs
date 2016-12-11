using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Forum3.DataModels {
	public class ApplicationUser : IdentityUser {
		public string DisplayName { get; set; }
		public DateTime Birthday { get; set; }
		public DateTime Registered { get; set; }
		public DateTime LastOnline { get; set; }
	}
}