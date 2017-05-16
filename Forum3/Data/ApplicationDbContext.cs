using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Forum3.Models.DataModels;

namespace Forum3.Data {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
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

		protected override void OnModelCreating(ModelBuilder builder) {
			base.OnModelCreating(builder);

			builder.Entity<Message>()
				.HasOne(r => r.PostedBy)
				.WithMany()
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Message>()
				.HasOne(r => r.EditedBy)
				.WithMany()
				.HasForeignKey(r => r.EditedById)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Message>()
				.HasOne(r => r.LastReplyBy)
				.WithMany()
				.HasForeignKey(r => r.LastReplyById)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<MessageBoard>()
				.HasOne(r => r.Message)
				.WithMany()
				.HasForeignKey(r => r.MessageId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<MessageBoard>()
				.HasOne(r => r.Board)
				.WithMany()
				.HasForeignKey(r => r.BoardId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<MessageThought>()
				.HasOne(r => r.Smiley)
				.WithMany()
				.HasForeignKey(r => r.SmileyId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<MessageThought>()
				.HasOne(r => r.Message)
				.WithMany(r => r.Thoughts)
				.HasForeignKey(r => r.MessageId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<MessageThought>()
				.HasOne(r => r.User)
				.WithMany()
				.HasForeignKey(r => r.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Board>()
				.HasOne(r => r.Category)
				.WithMany()
				.HasForeignKey(r => r.CategoryId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.Entity<Board>()
				.HasOne(r => r.LastMessage)
				.WithMany()
				.HasForeignKey(r => r.LastMessageId)
				.OnDelete(DeleteBehavior.Restrict);
		}
	}
}
