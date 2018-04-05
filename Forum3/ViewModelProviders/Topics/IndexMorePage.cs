using Forum3.Contexts;
using Forum3.Exceptions;
using Forum3.Processes.Topics;
using System.Linq;

namespace Forum3.ViewModelProviders.Topics {
	using PageModels = Models.ViewModels.Topics.Pages;

	public class IndexMorePage {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		LoadTopicPreview TopicPreviewLoader { get; }

		public IndexMorePage(
			ApplicationDbContext dbContext,
			UserContext userContext,
			LoadTopicPreview topicPreviewLoader
		) {
			DbContext = dbContext;
			UserContext = userContext;
			TopicPreviewLoader = topicPreviewLoader;
		}

		public PageModels.TopicIndexMorePage Generate(int boardId, long after, int unread) {
			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == boardId).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var topicPreviews = TopicPreviewLoader.Execute(boardId, after, unread);

			if (topicPreviews.Any())
				after = topicPreviews.Min(t => t.LastReplyPostedDT).Ticks;
			else
				after = long.MaxValue;

			return new PageModels.TopicIndexMorePage {
				More = after != long.MaxValue,
				After = after,
				Topics = topicPreviews
			};
		}
	}
}