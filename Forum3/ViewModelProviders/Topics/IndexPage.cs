using Forum3.Contexts;
using Forum3.Exceptions;
using Forum3.Processes.Topics;
using System.Linq;

namespace Forum3.ViewModelProviders.Topics {
	using PageModels = Models.ViewModels.Topics.Pages;

	public class IndexPage {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		LoadTopicPreview TopicPreviewLoader { get; }

		public IndexPage(
			ApplicationDbContext dbContext,
			UserContext userContext,
			LoadTopicPreview topicPreviewLoader
		) {
			DbContext = dbContext;
			UserContext = userContext;
			TopicPreviewLoader = topicPreviewLoader;
		}

		public PageModels.TopicIndexPage Generate(int boardId, int unread) {
			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == boardId).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var topicPreviews = TopicPreviewLoader.Execute(boardId, 0, unread);

			var after = 0L;

			if (topicPreviews.Any())
				after = topicPreviews.Min(t => t.LastReplyPostedDT).Ticks;

			var boardRecord = DbContext.Boards.Find(boardId);

			return new PageModels.TopicIndexPage {
				BoardId = boardId,
				BoardName = boardRecord?.Name ?? "All Topics",
				After = after,
				Topics = topicPreviews,
				UnreadFilter = unread
			};
		}
	}
}