using Forum.Models.Options;
using Forum.Services;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	[Authorize(Roles = Constants.InternalKeys.Admin)]
	public class V5 : Controller {
		ApplicationDbContext DbContext { get; }
		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public V5(
			ApplicationDbContext dbContext,
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<IActionResult> Migrate() {
			var take = 5;
			var topics = DbContext.Messages.Count(item => item.ParentId == 0);

			var viewModel = new ViewModels.MultiStep {
				ActionName = "Migration",
				ActionNote = "Creating topics from top level messages and migrating message artifacts.",
				Action = UrlHelper.Action(nameof(ContinueMigration)),
				Page = 0,
				TotalPages = Convert.ToInt32(Math.Floor(1d * topics / take)),
				Take = take,
			};

			return await ForumViewResult.ViewResult(this, "MultiStep", viewModel);
		}

		public async Task<IActionResult> ContinueMigration(InputModels.MultiStepInput input) {
			var parentMessagesQuery = from message in DbContext.Messages
									  where message.ParentId == 0
									  select message;

			parentMessagesQuery = parentMessagesQuery.Skip(input.Page * input.Take).Take(input.Take);

			foreach (var firstMessage in parentMessagesQuery) {
				DataModels.Message lastMessage = null;

				if (firstMessage.LastReplyId > 0) {
					lastMessage = DbContext.Messages.Find(firstMessage.LastReplyId);
				}

				var topic = new DataModels.Topic {
					FirstMessageId = firstMessage.Id,
					FirstMessagePostedById = firstMessage.PostedById,
					FirstMessageTimePosted = firstMessage.TimePosted,
					FirstMessageShortPreview = firstMessage.ShortPreview,
					LastMessageId = lastMessage?.Id ?? firstMessage.Id,
					LastMessagePostedById = lastMessage?.PostedById ?? firstMessage.PostedById,
					LastMessageTimePosted = lastMessage?.TimePosted ?? firstMessage.TimePosted,
					LastMessageShortPreview = lastMessage?.ShortPreview ?? firstMessage.ShortPreview,
					Pinned = firstMessage.Pinned,
					Deleted = firstMessage.Deleted,
					ReplyCount = firstMessage.ReplyCount,
					ViewCount = firstMessage.ViewCount
				};

				DbContext.Topics.Add(topic);

				await DbContext.SaveChangesAsync();

				await UpdateMessageArtifacts(topic.Id, firstMessage.Id);
			}

			return Ok();
		}

		async Task UpdateMessageArtifacts(int topicId, int parentMessageId) {
			var messagesQuery = from message in DbContext.Messages
								where message.Id == parentMessageId || message.ParentId == parentMessageId
								select message.Id;

			var pTopicId = new SqlParameter("@TopicId", topicId);
			var pViewLogTypeMessage = new SqlParameter("@ViewLogTypeMessage", EViewLogTargetType.Message);
			var pViewLogTypeTopic = new SqlParameter("@ViewLogTypeTopic", EViewLogTargetType.Topic);

			var pMessageIds = new SqlParameter("@MessageIds", await messagesQuery.ToListAsync());

			var updateTasks = new List<Task> {
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.ViewLogs)}] SET {nameof(DataModels.ViewLog.TargetId)} = @TopicId, {nameof(DataModels.ViewLog.TargetType)} = @ViewLogTypeTopic WHERE {nameof(DataModels.ViewLog.TargetType)} = @ViewLogTypeMessage AND {nameof(DataModels.ViewLog.TargetId)} IN @MessageIds", pTopicId, pViewLogTypeTopic, pViewLogTypeMessage, pMessageIds),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.TopicId)} = @TopicId WHERE {nameof(DataModels.Message.Id)} IN @MessageIds", pTopicId, pMessageIds),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.TopicBoards)}] SET {nameof(DataModels.TopicBoard.TopicId)} = @TopicId WHERE {nameof(DataModels.TopicBoard.MessageId)} IN @MessageIds", pTopicId, pMessageIds),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Participants)}] SET {nameof(DataModels.Participant.TopicId)} = @TopicId WHERE {nameof(DataModels.Participant.MessageId)} IN @MessageIds", pTopicId, pMessageIds),
				DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Bookmarks)}] SET {nameof(DataModels.Bookmark.TopicId)} = @TopicId WHERE {nameof(DataModels.Bookmark.MessageId)} IN @MessageIds", pTopicId, pMessageIds),
			};

			await Task.WhenAll(updateTasks);
			await DbContext.SaveChangesAsync();
		}
	}
}
