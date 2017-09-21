using Microsoft.EntityFrameworkCore;

namespace Forum3.Models.MigrationModels {
	public class MigrationDbContext : DbContext {
		public string ConnectionString { get; set; }

		public DbSet<Board> Boards { get; set; }
		public DbSet<BoardRelationship> BoardRelationships { get; set; }
		public DbSet<InviteOnlyTopicUsers> InviteOnlyTopicUsers { get; set; }
		public DbSet<Membership> Membership { get; set; }
		public DbSet<MessageBoard> MessageBoards { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<MessageThought> MessageThoughts { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<Participant> Participants { get; set; }
		public DbSet<Pin> Pins { get; set; }
		public DbSet<Role> Roles { get; set; }
		public DbSet<SiteSetting> SiteSettings { get; set; }
		public DbSet<Smiley> Smileys { get; set; }
		public DbSet<UserInRole> UsersInRoles { get; set; }
		public DbSet<UserProfile> UserProfiles { get; set; }
		public DbSet<ViewLog> ViewLogs { get; set; }

		public MigrationDbContext(string connectionString) {
			ConnectionString = connectionString;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			optionsBuilder.UseSqlServer(ConnectionString);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<UserInRole>()
				.HasKey(u => new { u.RoleId, u.UserId });
		}
	}
}