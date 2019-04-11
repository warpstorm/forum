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
		public IActionResult Install() {
			CheckInstallContext();

			return View("Process", new List<string> {
				Url.Action(nameof(InstallRoles)),
				Url.Action(nameof(InstallAdmins)),
				Url.Action(nameof(InstallCategories)),
				Url.Action(nameof(InstallBoards)),
			});
		}

		[HttpGet]
		public IActionResult MigrateV5() {
			return View("Process", new List<string> {
				Url.Action(nameof(CleanupDeletedMessages)),
				Url.Action(nameof(ResetMessageTopicId)),
				Url.Action(nameof(ResetViewLogs)),
				Url.Action(nameof(ResetTopicBoards)),
				Url.Action(nameof(DeleteTopics)),
				Url.Action(nameof(MigrateTopics)),
				Url.Action(nameof(MigrateMessages)),
				Url.Action(nameof(MigrateViewLogs)),
				Url.Action(nameof(MigrateBookmarks)),
				Url.Action(nameof(MigrateTopicBoards)),
				Url.Action(nameof(RebuildTopicReplies)),
				Url.Action(nameof(RebuildTopicParticipants)),
			});
		}

		[HttpGet]
		public IActionResult Maintenance() {
			return View("Process", new List<string> {
				Url.Action(nameof(CleanupDeletedMessages)),
				Url.Action(nameof(CleanupDeletedTopics)),
				Url.Action(nameof(RebuildTopicReplies)),
				Url.Action(nameof(RebuildTopicParticipants)),
			});
		}

		[HttpGet]
		public IActionResult CleanupDeletedTopics() => View("Process", new List<string> { Url.Action(nameof(CleanupDeletedTopics)) });

		[HttpPost]
		public async Task<IActionResult> CleanupDeletedTopics(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 1;
				var totalRecords = 1;
				var totalSteps = 1;

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Cleanup Deleted Topics",
					ActionNote = "Deleting topics marked for deletion.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			var pTrue = new SqlParameter("@True", true);
			await DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Topics)}] WHERE {nameof(DataModels.Topic.Deleted)} = @True", pTrue);

			return Ok();
		}

		[HttpGet]
		public IActionResult RebuildTopicReplies() => View("Process", new List<string> { Url.Action(nameof(RebuildTopicReplies)) });

		[HttpPost]
		public async Task<IActionResult> RebuildTopicReplies(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 10;
				var totalRecords = await DbContext.Topics.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Rebuild Topic Replies",
					ActionNote = "Recounting replies, determining first and last messages.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();
			var lastRecordId = 0;

			foreach (var record in records) {
				await TopicRepository.RebuildTopicReplies(record);
				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpGet]
		public IActionResult RebuildTopicParticipants() => View("Process", new List<string> { Url.Action(nameof(RebuildTopicParticipants)) });

		[HttpPost]
		public async Task<IActionResult> RebuildTopicParticipants(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 20;
				var totalRecords = await DbContext.Topics.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Rebuild Topic Participants",
					ActionNote = "Identifying topics participants.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var recordIds = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).ToListAsync();
			var lastRecordId = 0;

			foreach (var recordId in recordIds) {
				var messagesQuery = from message in DbContext.Messages
									where message.TopicId == recordId
									select new {
										message.PostedById,
										message.TimePosted
									};

				var messages = await messagesQuery.ToListAsync();

				var newParticipants = new List<DataModels.Participant>();

				foreach (var message in messages) {
					if (!newParticipants.Any(item => item.UserId == message.PostedById)) {
						newParticipants.Add(new DataModels.Participant {
							TopicId = recordId,
							UserId = message.PostedById,
							Time = message.TimePosted
						});
					}
				}

				var oldParticipants = await DbContext.Participants.Where(r => r.TopicId == recordId).ToListAsync();

				DbContext.RemoveRange(oldParticipants);
				DbContext.Participants.AddRange(newParticipants);
				await DbContext.SaveChangesAsync();

				lastRecordId = recordId;
			}

			return Ok(lastRecordId);
		}

		[HttpGet]
		public IActionResult CleanupDeletedMessages() => View("Process", new List<string> { Url.Action(nameof(CleanupDeletedMessages)) });

		[HttpPost]
		public async Task<IActionResult> CleanupDeletedMessages(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 1;
				var totalRecords = 1;
				var totalSteps = 1;

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Cleanup Deleted Messages",
					ActionNote = "Deleting messages marked for deletion.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			var pTrue = new SqlParameter("@True", true);
			await DbContext.Database.ExecuteSqlCommandAsync($"DELETE FROM [{nameof(ApplicationDbContext.Messages)}] WHERE {nameof(DataModels.Message.Deleted)} = @True", pTrue);

			return Ok();
		}

		[HttpGet]
		public IActionResult ReprocessMessages() => View("Process", new List<string> { Url.Action(nameof(ReprocessMessages)) });

		[HttpPost]
		public async Task<IActionResult> ReprocessMessages(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 10;
				var totalRecords = DbContext.Messages.Count();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Reprocessing Messages",
					ActionNote = "Message contents are rebuilt, links re-checked, BBC reprocessed.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();

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

		[HttpPost]
		public async Task<IActionResult> InstallRoles(ControllerModels.Administration.ProcessStep input) {
			CheckInstallContext();

			if (input.CurrentStep < 0) {
				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Roles",
					ActionNote = "Setting up roles.",
					Take = 1,
					TotalSteps = 1,
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
		public async Task<IActionResult> InstallAdmins(ControllerModels.Administration.ProcessStep input) {
			CheckInstallContext();

			if (input.CurrentStep < 0) {
				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Admin",
					ActionNote = "Registering administrator account.",
					Take = 1,
					TotalSteps = 1,
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
		public async Task<IActionResult> InstallCategories(ControllerModels.Administration.ProcessStep input) {
			CheckInstallContext();

			if (input.CurrentStep < 0) {
				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Categories",
					ActionNote = "Creating categories.",
					Take = 1,
					TotalSteps = 1,
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
		public async Task<IActionResult> InstallBoards(ControllerModels.Administration.ProcessStep input) {
			CheckInstallContext();

			if (input.CurrentStep < 0) {
				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Boards",
					ActionNote = "Creating boards.",
					Take = 1,
					TotalSteps = 1,
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

		[HttpPost]
		public async Task<IActionResult> ResetMessageTopicId(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 200;
				var totalRecords = await DbContext.Messages.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Reset TopicId Column",
					ActionNote = "Resetting TopicId column on all messages.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var recordQuery = DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take);

			var lastRecordId = 0;

			foreach (var record in recordQuery) {
				record.TopicId = 0;
				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> ResetViewLogs(ControllerModels.Administration.ProcessStep input) {
			var recordsQuery = DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Topic);

			if (input.CurrentStep < 0) {
				var take = 200;
				var totalRecords = await recordsQuery.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Reset ViewLogs",
					ActionNote = "Re-associating viewlogs back to message IDs.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await recordsQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await recordsQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();
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
		public async Task<IActionResult> ResetTopicBoards(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 200;
				var totalRecords = await DbContext.TopicBoards.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Reset TopicBoards",
					ActionNote = "Re-associating topic boards back to message IDs.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.TopicBoards.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await DbContext.TopicBoards.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();

			var lastRecordId = 0;

			foreach (var record in records) {
				var topic = DbContext.Topics.Find(record.TopicId);

				if (topic is null) {
					var message = DbContext.Messages.Find(record.MessageId);
					topic = DbContext.Topics.Find(message.TopicId);
				}

				if (!(topic is null)) {
					record.MessageId = topic.FirstMessageId;

					DbContext.Update(record);
					lastRecordId = record.Id;
				}
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteTopics(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 50;
				var totalRecords = await DbContext.Topics.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Delete Topics",
					ActionNote = "Removing all previously created topics.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			var records = DbContext.Topics.Take(input.Take);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();

			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> MigrateTopics(ControllerModels.Administration.ProcessStep input) {
			var parentMessagesQuery = from message in DbContext.Messages
									  where message.ParentId == 0
									  select message;

			if (input.CurrentStep < 0) {
				var take = 10;
				var totalRecords = await parentMessagesQuery.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Migrate Topics",
					ActionNote = "Creating topics from top level messages.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await parentMessagesQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await parentMessagesQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();

			var lastRecordId = 0;

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

				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> MigrateMessages(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 100;
				var totalRecords = await DbContext.Messages.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Migrate Messages",
					ActionNote = "Associate messages to topics.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = DbContext.Messages.Where(item => item.Id > input.LastRecordId).Take(input.Take);

			var lastRecordId = 0;

			foreach (var record in records) {
				var targetId = record.ParentId == 0 ? record.Id : record.ParentId;
				record.TopicId = DbContext.Topics.First(item => item.FirstMessageId == targetId).Id;
				lastRecordId = record.Id;
			}

			await DbContext.SaveChangesAsync();

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> MigrateViewLogs(ControllerModels.Administration.ProcessStep input) {
			var recordsQuery = DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Message);

			if (input.CurrentStep < 0) {
				var take = 200;
				var totalRecords = await recordsQuery.CountAsync();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Migrate ViewLogs",
					ActionNote = "Updating viewlogs from messages to topics.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await recordsQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await recordsQuery.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();
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
		public async Task<IActionResult> MigrateBookmarks(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 50;
				var totalRecords = DbContext.Topics.Count();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Migrate Bookmarks",
					ActionNote = "Updating bookmarks from messages to topics.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).ToListAsync();

			var lastRecordId = 0;

			foreach (var record in records) {
				var pTopicId = new SqlParameter("@TopicId", record.Id);
				var pFirstMessageId = new SqlParameter("@FirstMessageId", record.FirstMessageId);

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

				lastRecordId = record.Id;
			}

			return Ok(lastRecordId);
		}

		[HttpPost]
		public async Task<IActionResult> MigrateTopicBoards(ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				var take = 50;
				var totalRecords = DbContext.Topics.Count();
				var totalSteps = Convert.ToInt32(Math.Floor(1d * totalRecords / take));

				return Ok(new ControllerModels.Administration.ProcessStage {
					ActionName = "Migrate TopicBoards",
					ActionNote = "Updating TopicBoards from messages to topics and removing duplicates.",
					Take = take,
					TotalSteps = totalSteps,
					TotalRecords = totalRecords,
				});
			}

			if (input.LastRecordId < 0) {
				var step = 0;

				while (step < input.CurrentStep) {
					input.LastRecordId = await DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take).Select(item => item.Id).LastAsync();
					step++;
				}
			}

			var records = DbContext.Topics.Where(item => item.Id > input.LastRecordId).Take(input.Take);

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
						MessageId = record.FirstMessageId,
						TopicId = record.Id,
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
