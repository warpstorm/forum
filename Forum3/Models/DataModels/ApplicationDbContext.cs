using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Forum3.Models.DataModels {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string> {
		public DbSet<Board> Boards { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<MessageBoard> MessageBoards { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<MessageThought> MessageThoughts { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<Participant> Participants { get; set; }
		public DbSet<Pin> Pins { get; set; }
		public DbSet<SiteSetting> SiteSettings { get; set; }
		public DbSet<Smiley> Smileys { get; set; }
		public DbSet<ViewLog> ViewLogs { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Message>()
				.HasIndex(b => b.LastReplyPosted);
		}
	}
}