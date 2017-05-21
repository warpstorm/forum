using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Forum3.Models.DataModels {
	public class ApplicationRole : IdentityRole {
		public string Description { get; set; }
		public DateTime CreatedDate { get; set; }
		public string UserId { get; set; }
	}
}