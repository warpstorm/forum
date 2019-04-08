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
		MessageRepository MessageRepository { get; }
		RoleRepository RoleRepository { get; }
		TopicRepository TopicRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Administration(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			MessageRepository messageRepository,
			RoleRepository roleRepository,
			TopicRepository topicRepository,
			IForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			MessageRepository = messageRepository;
			RoleRepository = roleRepository;
			TopicRepository = topicRepository;
			ForumViewResult = forumViewResult;
		}

		[HttpGet]
		public async Task<IActionResult> Maintenance() {
			return await ForumViewResult.ViewResult(this, "MultiStep", new List<string> {
				Url.Action(nameof(CleanupDeletedMessages)),
				Url.Action(nameof(ReprocessMessages)),
				Url.Action(nameof(RebuildTopics)),
			});
		}

		[HttpPost]
		public async Task<IActionResult> RebuildTopics(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 50;
				var totalRecords = await DbContext.Topics.CountAsync();
				var totalPages = 1;

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Rebuild Topics",
					ActionNote = "Recounting replies, calculating participants, determining first and last messages, deleting where necessary.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();

			if (input.LastRecordId < 0) {
				records = await DbContext.Topics.Skip(input.CurrentPage * input.Take).Take(input.Take).ToListAsync();
			}

			var lastRecordId = 0;

			foreach (var record in records) {
				await TopicRepository.RebuildTopic(record);
				lastRecordId = record.Id;
			}

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> CleanupDeletedMessages(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 1;
				var totalRecords = 1;
				var totalPages = 1;

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Cleanup Deleted Messages",
					ActionNote = "Deleting messages marked for deletion.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var pTrue = new SqlParameter("@True", true);
			await DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Messages)}] WHERE {nameof(DataModels.Message.Deleted)} = @True", pTrue);

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> ReprocessMessages(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 10;
				var totalRecords = DbContext.Messages.Count();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Reprocessing Messages",
					ActionNote = "Message contents are rebuilt, links re-checked, BBC reprocessed.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = await DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();

			if (input.LastRecordId < 0) {
				records = await DbContext.Messages.Skip(input.CurrentPage * input.Take).Take(input.Take).ToListAsync();
			}

			var lastRecordId = 0;

			foreach (var record in records) {
				var processedMessage = await MessageRepository.ProcessMessageInput(record.OriginalBody);

				if (!processedMessage.Errors.Any()) {
					record.OriginalBody = processedMessage.OriginalBody;
					record.DisplayBody = processedMessage.DisplayBody;
					record.ShortPreview = processedMessage.ShortPreview;
					record.LongPreview = processedMessage.LongPreview;
					record.Cards = processedMessage.Cards;

					DbContext.Update(record);
				}

				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpGet]
		public async Task<IActionResult> Install() {
			CheckInstallContext();

			return await ForumViewResult.ViewResult(this, "MultiStep", new List<string> {
				Url.Action(nameof(InstallRoles)),
				Url.Action(nameof(InstallAdmins)),
				Url.Action(nameof(InstallCategories)),
				Url.Action(nameof(InstallBoards)),
			});
		}

		[HttpPost]
		public async Task<IActionResult> InstallRoles(ControllerModels.Administration.Page input) {
			CheckInstallContext();

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
		public async Task<IActionResult> InstallAdmins(ControllerModels.Administration.Page input) {
			CheckInstallContext();

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
		public async Task<IActionResult> InstallCategories(ControllerModels.Administration.Page input) {
			CheckInstallContext();

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
		public async Task<IActionResult> InstallBoards(ControllerModels.Administration.Page input) {
			CheckInstallContext();

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

		[HttpGet]
		public async Task<IActionResult> MigrateV5() {
			return await ForumViewResult.ViewResult(this, "MultiStep", new List<string> {
				Url.Action(nameof(CleanupDeletedMessages)),
				Url.Action(nameof(ResetMessageTopicId)),
				Url.Action(nameof(ResetViewLogs)),
				Url.Action(nameof(DeleteTopics)),
				Url.Action(nameof(MigrateTopics)),
				Url.Action(nameof(MigrateMessages)),
				Url.Action(nameof(MigrateViewLogs)),
				Url.Action(nameof(MigrateBookmarks)),
				Url.Action(nameof(MigrateParticipants)),
				Url.Action(nameof(MigrateTopicBoards)),
				Url.Action(nameof(ReprocessMessages)),
				Url.Action(nameof(RebuildTopics)),
			});
		}

		[HttpPost]
		public async Task<IActionResult> ResetMessageTopicId(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 100;
				var totalRecords = await DbContext.Messages.CountAsync();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Reset TopicId Column",
					ActionNote = "Resetting TopicId column on all messages.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var recordQuery = DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take);

			if (input.LastRecordId < 0) {
				recordQuery = DbContext.Messages.Skip(input.Take * input.CurrentPage).Take(input.Take);
			}

			var lastRecordId = 0;

			foreach (var record in recordQuery) {
				record.TopicId = 0;
				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> ResetViewLogs(ControllerModels.Administration.Page input) {
			var recordsQuery = DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Topic);

			if (input.CurrentPage < 0) {
				var take = 500;
				var totalRecords = await recordsQuery.CountAsync();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Reset ViewLogs",
					ActionNote = "Re-associating viewlogs back to message IDs.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}


			if (input.LastRecordId >= 0) {
				recordsQuery = recordsQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take);
			}
			else {
				recordsQuery = recordsQuery.Skip(input.CurrentPage * input.Take).Take(input.Take);
			}

			var records = await recordsQuery.ToListAsync();
			var topicIds = records.Select(item => item.TargetId).Distinct();

			var messageIdsQuery = from topic in DbContext.Topics
								where topicIds.Contains(topic.Id)
								select new {
									MessageId = topic.FirstMessageId,
									TopicId = topic.Id
								};

			var messageIds = await messageIdsQuery.ToListAsync();

			var lastRecordId = 0;

			foreach (var record in records) {
				record.TargetType = EViewLogTargetType.Message;
				record.TargetId = messageIds.First(item => item.TopicId == record.TargetId).MessageId;
				DbContext.Update(record);
				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteTopics(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 500;
				var totalRecords = await DbContext.Topics.CountAsync();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Delete Topics",
					ActionNote = "Removing all previously created topics.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = DbContext.Topics.Take(input.Take);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> MigrateTopics(ControllerModels.Administration.Page input) {
			var parentMessagesQuery = from message in DbContext.Messages
									  where message.ParentId == 0 && message.TopicId == 0
									  select message;

			if (input.CurrentPage < 0) {
				var take = 100;
				var totalRecords = await parentMessagesQuery.CountAsync();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migrate Topics",
					ActionNote = "Creating topics from top level messages.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = parentMessagesQuery.Take(input.Take);

			foreach (var record in records) {
				DbContext.Topics.Add(new DataModels.Topic {
					FirstMessageId = record.Id,
					FirstMessagePostedById = record.PostedById,
					FirstMessageTimePosted = record.TimePosted,
					FirstMessageShortPreview = record.ShortPreview,
					Pinned = record.Pinned,
					Deleted = record.Deleted,
					ViewCount = record.ViewCount
				});
			}

			await DbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> MigrateMessages(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 100;
				var totalRecords = await DbContext.Messages.CountAsync();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migrate Messages",
					ActionNote = "Associate messages to topics.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take);

			if (input.LastRecordId < 0) {
				records = DbContext.Messages.Skip(input.CurrentPage * input.Take).Take(input.Take);
			}

			var lastRecordId = 0;

			foreach (var record in records) {
				record.TopicId = DbContext.Topics.First(item => record.ParentId == 0 ? item.FirstMessageId == record.Id : item.FirstMessageId == record.ParentId).Id;
				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> MigrateViewLogs(ControllerModels.Administration.Page input) {
			var recordsQuery = DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Message);

			if (input.CurrentPage < 0) {
				var take = 500;
				var totalRecords = await recordsQuery.CountAsync();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migrate ViewLogs",
					ActionNote = "Updating viewlogs from messages to topics.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId >= 0) {
				recordsQuery = recordsQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take);
			}
			else {
				recordsQuery = recordsQuery.Skip(input.CurrentPage * input.Take).Take(input.Take);
			}

			var records = await recordsQuery.ToListAsync();
			var messageIds = records.Select(item => item.TargetId).Distinct();

			var topicIdsQuery = from message in DbContext.Messages
								where messageIds.Contains(message.Id)
								select new {
									MessageId = message.Id,
									message.TopicId
								};

			var topicIds = await topicIdsQuery.ToListAsync();

			var lastRecordId = 0;

			foreach (var record in records) {
				var firstItem = topicIds.FirstOrDefault(item => item.MessageId == record.TargetId);

				if (firstItem is null) {
					DbContext.Remove(record);
				}
				else {
					record.TargetType = EViewLogTargetType.Topic;
					record.TargetId = topicIds.First(item => item.MessageId == record.TargetId).TopicId;
					DbContext.Update(record);
					lastRecordId = record.Id;
				}
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> MigrateBookmarks(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 50;
				var totalRecords = DbContext.Topics.Count();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migrate Bookmarks",
					ActionNote = "Updating bookmarks from messages to topics.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var topics = DbContext.Topics.Skip(input.CurrentPage * input.Take).Take(input.Take);

			foreach (var topic in topics) {
				var pTopicId = new SqlParameter("@TopicId", topic.Id);
				var pFirstMessageId = new SqlParameter("@FirstMessageId", topic.FirstMessageId);

				await DbContext.Database.ExecuteSqlCommandAsync($@"
					UPDATE [{nameof(ApplicationDbContext.Bookmarks)}]
					SET {nameof(DataModels.Bookmark.TopicId)} = @TopicId
					WHERE {nameof(DataModels.Bookmark.MessageId)}
					IN (
						SELECT Id
						FROM [{nameof(ApplicationDbContext.Messages)}]
						WHERE Id = @FirstMessageId
						OR ParentId = @FirstMessageId
					)", pTopicId, pFirstMessageId);
			}

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> MigrateParticipants(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 100;
				var totalRecords = DbContext.Participants.Count();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migrate Participants",
					ActionNote = "Updating participants from messages to topics.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = DbContext.Participants.Where(item => item.Id > input.LastRecordId).Take(input.Take);

			if (input.LastRecordId < 0) {
				records = DbContext.Participants.Skip(input.CurrentPage * input.Take).Take(input.Take);
			}

			var lastRecordId = 0;

			foreach (var record in records) {
				var message = await DbContext.Messages.FirstOrDefaultAsync(item => item.Id == record.MessageId);

				if (message is null) {
					DbContext.Participants.Remove(record);
				}
				else {
					record.TopicId = message.TopicId;
				}

				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> MigrateTopicBoards(ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				var take = 500;
				var totalRecords = DbContext.Topics.Count();
				var totalPages = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.Step {
					ActionName = "Migrate TopicBoards",
					ActionNote = "Updating TopicBoards from messages to topics and removing duplicates.",
					Take = take,
					TotalPages = totalPages,
					TotalRecords = totalRecords,
				});
			}

			var records = DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take);

			if (input.LastRecordId < 0) {
				records = DbContext.Topics.Skip(input.CurrentPage * input.Take).Take(input.Take);
			}

			var lastRecordId = 0;

			foreach (var record in records) {
				var topicBoardsQuery = from topicBoard in DbContext.TopicBoards
									   where topicBoard.MessageId == record.FirstMessageId
									   select topicBoard;

				var topicBoards = await topicBoardsQuery.ToListAsync();
				var topicBoardIds = topicBoards.Select(item => item.BoardId).Distinct();
				var newTopicBoards = new List<DataModels.TopicBoard>();

				foreach (var topicBoardId in topicBoardIds) {
					var firstTopicBoard = topicBoards.First(item => item.BoardId == topicBoardId);

					newTopicBoards.Add(new DataModels.TopicBoard {
						BoardId = firstTopicBoard.BoardId,
						MessageId = firstTopicBoard.MessageId,
						TopicId = firstTopicBoard.TopicId,
						TimeAdded = firstTopicBoard.TimeAdded,
						UserId = firstTopicBoard.UserId
					});
				}

				DbContext.RemoveRange(topicBoards);
				DbContext.AddRange(newTopicBoards);

				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		void CheckInstallContext() {
			if (!UserContext.IsAuthenticated) {
				throw new HttpException("You must create an account and log into it first.");
			}

			if (DbContext.Users.Count() > 1) {
				throw new HttpException("This process can only run when there's one user registered.");
			}
		}
	}
}
