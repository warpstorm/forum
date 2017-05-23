using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Forum3.Models.DataModels {
	public class ApplicationRole : IdentityRole {
		public string Description { get; set; }
		public DateTime CreatedDate { get; set; }
		public string CreatedById { get; set; }
		public DateTime ModifiedDate { get; set; }
		public string ModifiedById { get; set; }
	}
}