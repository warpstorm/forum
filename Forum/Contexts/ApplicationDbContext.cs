using Forum.Enums;
using Forum.Models.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Forum.Contexts {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string> {
		public DbSet<Board> Boards { get; set; }
		public DbSet<BoardRole> BoardRoles { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<MessageBoard> MessageBoards { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<MessageThought> MessageThoughts { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<Participant> Participants { get; set; }
		public DbSet<Pin> Pins { get; set; }
		public DbSet<Quote> Quotes { get; set; }
		public DbSet<SiteSetting> SiteSettings { get; set; }
		public DbSet<Smiley> Smileys { get; set; }
		public DbSet<StrippedUrl> StrippedUrls { get; set; }
		public DbSet<ViewLog> ViewLogs { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<ApplicationUser>()
				.HasIndex(r => r.DisplayName);

			modelBuilder.Entity<ApplicationUser>()
				.Property(r => r.FrontPage)
				.HasDefaultValue(EFrontPage.Boards);

			modelBuilder.Entity<ApplicationUser>()
				.Property(r => r.MessagesPerPage)
				.HasDefaultValue(25);

			modelBuilder.Entity<ApplicationUser>()
				.Property(r => r.PopularityLimit)
				.HasDefaultValue(30);

			modelBuilder.Entity<ApplicationUser>()
				.Property(r => r.ShowFavicons)
				.HasDefaultValue(true);

			modelBuilder.Entity<ApplicationUser>()
				.Property(r => r.TopicsPerPage)
				.HasDefaultValue(15);

			modelBuilder.Entity<BoardRole>()
				.HasIndex(r => r.BoardId);

			modelBuilder.Entity<MessageBoard>()
				.HasIndex(r => r.BoardId);

			modelBuilder.Entity<MessageBoard>()
				.HasIndex(r => r.MessageId);

			modelBuilder.Entity<Message>()
				.HasIndex(r => r.Processed);

			modelBuilder.Entity<Message>()
				.HasIndex(r => r.LastReplyPosted);

			modelBuilder.Entity<Pin>()
				.HasIndex(r => r.MessageId);

			modelBuilder.Entity<Pin>()
				.HasIndex(r => r.UserId);

			modelBuilder.Entity<Quote>()
				.HasIndex(r => r.Approved);

			modelBuilder.Entity<SiteSetting>()
				.HasIndex(r => new { r.Name, r.UserId });
		}
	}
}