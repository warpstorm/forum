using Forum3.DataModels;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;

namespace Forum3.Data {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
		public DbSet<Message> Messages { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


        }
    }
}
