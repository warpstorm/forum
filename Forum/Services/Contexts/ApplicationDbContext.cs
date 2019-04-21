using Forum.Models.Options;
using Forum.Models.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Forum.Services.Contexts {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string> {
		public DbSet<ActionLogItem> ActionLog { get; set; }
		public DbSet<Board> Boards { get; set; }
		public DbSet<BoardRole> BoardRoles { get; set; }
		public DbSet<Bookmark> Bookmarks { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<MessageThought> MessageThoughts { get; set; }
		public DbSet<Notification> Notifications { get; set; }
		public DbSet<Participant> Participants { get; set; }
		public DbSet<Quote> Quotes { get; set; }
		public DbSet<Smiley> Smileys { get; set; }
		public DbSet<StrippedUrl> StrippedUrls { get; set; }
		public DbSet<Topic> Topics { get; set; }
		public DbSet<TopicBoard> TopicBoards { get; set; }
		public DbSet<ViewLog> ViewLogs { get; set; }

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		/// <summary>
		/// Standard SaveChanges, with a blunt-force concurrency exception handler to retry 3 times before failing.
		/// </summary>
		public override int SaveChanges() {
			var attempts = 0;

			while (true) {
				try {
					attempts++;
					return base.SaveChanges();
				}
				catch (DbUpdateConcurrencyException ex) when (attempts <= 3) {
					foreach (var entry in ex.Entries) {
						entry.Reload();
					}
				}
			}
		}

		/// <summary>
		/// Standard SaveChangesAsync, with a blunt-force concurrency exception handler to retry 3 times before failing.
		/// </summary>
		public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
			var attempts = 0;

			while (true) {
				try {
					attempts++;
					return await base.SaveChangesAsync(cancellationToken);
				}
				catch (DbUpdateConcurrencyException ex) when (attempts <= 3) {
					foreach (var entry in ex.Entries) {
						await entry.ReloadAsync();
					}
				}
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<ActionLogItem>()
				.HasIndex(r => r.UserId);

			modelBuilder.Entity<ActionLogItem>()
				.Property(r => r.Arguments)
				.HasConversion(
					v => JsonConvert.SerializeObject(v),
					v => JsonConvert.DeserializeObject<Dictionary<string, object>>(v)
				);

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
				.HasDefaultValue(7);

			modelBuilder.Entity<BoardRole>()
				.HasIndex(r => r.BoardId);

			modelBuilder.Entity<Bookmark>()
				.HasIndex(r => r.TopicId);

			modelBuilder.Entity<Bookmark>()
				.HasIndex(r => r.UserId);

			modelBuilder.Entity<Message>()
				.HasIndex(r => r.Deleted);

			modelBuilder.Entity<Message>()
				.HasIndex(r => r.TopicId);

			modelBuilder.Entity<Message>()
				.HasIndex(r => new { r.TopicId, r.Deleted });

			modelBuilder.Entity<Message>()
				.HasIndex(r => r.ReplyId);

			modelBuilder.Entity<Message>()
				.HasIndex(r => r.PostedById);

			modelBuilder.Entity<MessageThought>()
				.HasIndex(r => r.MessageId);

			modelBuilder.Entity<Notification>()
				.HasIndex(r => r.UserId);

			modelBuilder.Entity<Notification>()
				.HasIndex(r => new { r.MessageId, r.Type });

			modelBuilder.Entity<Participant>()
				.HasIndex(r => r.TopicId);

			modelBuilder.Entity<Participant>()
				.HasIndex(r => r.UserId);

			modelBuilder.Entity<Participant>()
				.HasIndex(r => new { r.UserId, r.TopicId });

			modelBuilder.Entity<Quote>()
				.HasIndex(r => r.Approved);

			modelBuilder.Entity<Topic>()
				.HasIndex(r => r.Deleted);

			modelBuilder.Entity<Topic>()
				.HasIndex(r => r.FirstMessageId);

			modelBuilder.Entity<Topic>()
				.HasIndex(r => r.LastMessageTimePosted);

			modelBuilder.Entity<TopicBoard>()
				.HasIndex(r => r.BoardId);

			modelBuilder.Entity<TopicBoard>()
				.HasIndex(r => r.TopicId);

			modelBuilder.Entity<ViewLog>()
				.HasIndex(r => new { r.UserId, r.LogTime });

			modelBuilder.Entity<ViewLog>()
				.HasIndex(r => new { r.UserId, r.TargetType, r.TargetId });

			modelBuilder.Entity<ViewLog>()
				.HasIndex(r => r.LogTime);
		}
	}
}