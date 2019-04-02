using Forum.Models.Errors;
using Forum.Models.Options;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;

	public class Administration : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		RoleRepository RoleRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Administration(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			RoleRepository roleRepository,
			IForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			RoleRepository = roleRepository;
			ForumViewResult = forumViewResult;
		}

		public async Task<IActionResult> Setup() {
			CheckContext();

			return await ForumViewResult.ViewResult(this, "MultiStep", new List<string> {
				Url.Action(nameof(SetupRoles)),
				Url.Action(nameof(SetupAdmins)),
				Url.Action(nameof(SetupCategories)),
				Url.Action(nameof(SetupBoards)),
			});
		}

		public async Task<IActionResult> Migrate() {
			return await ForumViewResult.ViewResult(this, "MultiStep", new List<string> {
				Url.Action(nameof(ContinueMigration))
			});
		}

		[HttpPost]
		public async Task<IActionResult> ContinueMigration(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 100;
				var topics = DbContext.Messages.Count(item => item.ParentId == 0);
				var totalPages = Convert.ToInt32(Math.Floor(1d * topics / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migration",
					ActionNote = "Creating topics from top level messages and migrating message artifacts.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = topics,
				});
			}

			var parentMessagesQuery = from message in DbContext.Messages
									  where message.ParentId == 0
									  select message;

			var parentMessages = await parentMessagesQuery.Skip(input.CurrentPage * input.Take).Take(input.Take).ToListAsync();

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

		[HttpPost]
		public async Task<IActionResult> SetupRoles(ControllerModels.Administration.Page input) {
			CheckContext();

			if (input.CurrentPage < 0) {
				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Roles",
					ActionNote = "Setting up roles.",
					Take = 1,
					TotalPages = 1,
					TotalRecords = 1,
				});
			}

			if (!(await RoleRepository.SiteRoles()).Any()) {
				await RoleRepository.Create(new InputModels.CreateRoleInput {
					Name = Constants.InternalKeys.Admin,
					Description = "Forum administrators"
				});
			}

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> SetupAdmins(ControllerModels.Administration.Page input) {
			CheckContext();

			if (input.CurrentPage < 0) {
				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Admin",
					ActionNote = "Registering administrator account.",
					Take = 1,
					TotalPages = 1,
					TotalRecords = 1,
				});
			}

			if (UserContext.IsAdmin) {
				return Ok();
			}

			var adminRole = (await RoleRepository.SiteRoles()).First(r => r.Name == Constants.InternalKeys.Admin);
			await RoleRepository.AddUser(adminRole.Id, UserContext.ApplicationUser.Id);

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> SetupCategories(ControllerModels.Administration.Page input) {
			CheckContext();

			if (input.CurrentPage < 0) {
				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Categories",
					ActionNote = "Creating categories.",
					Take = 1,
					TotalPages = 1,
					TotalRecords = 1,
				});
			}

			if (DbContext.Categories.Any()) {
				return Ok();
			}

			DbContext.Categories.Add(new DataModels.Category {
				DisplayOrder = 1,
				Name = "On Topic"
			});

			await DbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> SetupBoards(ControllerModels.Administration.Page input) {
			CheckContext();

			if (input.CurrentPage < 0) {
				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Boards",
					ActionNote = "Creating boards.",
					Take = 1,
					TotalPages = 1,
					TotalRecords = 1,
				});
			}

			if (DbContext.Boards.Any()) {
				return Ok();
			}

			var category = DbContext.Categories.First();

			DbContext.Boards.Add(new DataModels.Board {
				CategoryId = category.Id,
				DisplayOrder = 1,
				Name = "General Discussion",
				Description = "Various talk about things that interest you."
			});

			await DbContext.SaveChangesAsync();

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

			if (DbContext.Users.Count() > 1) {
				throw new HttpException("This process can only run when there's one user registered.");
			}
		}
	}
}
