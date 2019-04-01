using Forum.Models.Errors;
using Forum.Models.Options;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
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

	public class Setup : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		SetupService SetupService { get; }
		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Setup(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			SetupService setupService,
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			SetupService = setupService;
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public async Task<IActionResult> Initialize() {
			CheckContext();

			var totalPages = 4;

			var viewModel = new ViewModels.Delay {
				ActionName = "Initializing",
				ActionNote = "Beginning the setup process",
				CurrentPage = 0,
				TotalPages = totalPages,
				NextAction = UrlHelper.Action(nameof(Setup.Process), nameof(Setup), new InputModels.Continue { CurrentStep = 1, TotalSteps = totalPages })
			};

			return await ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		public async Task<IActionResult> Process(InputModels.Continue input) {
			CheckContext();

			var note = string.Empty;

			switch (input.CurrentStep) {
				case 1:
					note = "Roles have been setup.";
					await SetupService.SetupRoles();
					break;

				case 2:
					note = "Admins have been added.";
					await SetupService.SetupAdmins();
					break;

				case 3:
					note = "The first category has been added.";
					SetupService.SetupCategories();
					break;

				case 4:
					note = "The first board has been added.";
					SetupService.SetupBoards();
					break;
			}

			input.CurrentStep++;

			var viewModel = new ViewModels.Delay {
				ActionName = "Processing",
				ActionNote = note,
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Setup.Process), nameof(Setup), input)
			};

			if (input.CurrentStep > input.TotalSteps) {
				viewModel.NextAction = "/";
			}

			return await ForumViewResult.ViewResult(this, "Delay", viewModel);
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
				TotalRecords = topics,
				Take = take,
			};

			return await ForumViewResult.ViewResult(this, "MultiStep", viewModel);
		}

		public async Task<IActionResult> ContinueMigration(InputModels.MultiStepInput input) {
			var parentMessagesQuery = from message in DbContext.Messages
									  where message.ParentId == 0
									  select message;

			var parentMessages = await parentMessagesQuery.Skip(input.Page * input.Take).Take(input.Take).ToListAsync();

			foreach (var firstMessage in parentMessages) {
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

			var messageIds = await messagesQuery.ToListAsync();
			var messageIdsString = $"{string.Join(", ", messageIds)}";

			var pTopicId = new SqlParameter("@TopicId", topicId);
			var pViewLogTypeMessage = new SqlParameter("@ViewLogTypeMessage", EViewLogTargetType.Message);
			var pViewLogTypeTopic = new SqlParameter("@ViewLogTypeTopic", EViewLogTargetType.Topic);

			await DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.ViewLogs)}] SET {nameof(DataModels.ViewLog.TargetId)} = @TopicId, {nameof(DataModels.ViewLog.TargetType)} = @ViewLogTypeTopic WHERE {nameof(DataModels.ViewLog.TargetType)} = @ViewLogTypeMessage AND {nameof(DataModels.ViewLog.TargetId)} IN ({messageIdsString})", pTopicId, pViewLogTypeTopic, pViewLogTypeMessage);
			await DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Messages)}] SET {nameof(DataModels.Message.TopicId)} = @TopicId WHERE {nameof(DataModels.Message.Id)} IN ({messageIdsString})", pTopicId);
			await DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.TopicBoards)}] SET {nameof(DataModels.TopicBoard.TopicId)} = @TopicId WHERE {nameof(DataModels.TopicBoard.MessageId)} IN ({messageIdsString})", pTopicId);
			await DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Participants)}] SET {nameof(DataModels.Participant.TopicId)} = @TopicId WHERE {nameof(DataModels.Participant.MessageId)} IN ({messageIdsString})", pTopicId);
			await DbContext.Database.ExecuteSqlCommandAsync($"UPDATE [{nameof(ApplicationDbContext.Bookmarks)}] SET {nameof(DataModels.Bookmark.TopicId)} = @TopicId WHERE {nameof(DataModels.Bookmark.MessageId)} IN ({messageIdsString})", pTopicId);
		}

		void CheckContext() {
			if (!UserContext.IsAuthenticated) {
				throw new HttpException("You must create an account and log into it first.");
			}

			if (!UserContext.IsAdmin && DbContext.Users.Count() > 1) {
				throw new HttpException("Non-admins can only run this process when there's one user registered.");
			}
		}
	}
}
