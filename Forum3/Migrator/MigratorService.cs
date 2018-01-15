using Forum3.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Migrator {
	using DataModels = Forum3.Models.DataModels;
	using InputModels = Forum3.Models.InputModels;
	using MigratorModels = Models;
	using ViewModels = Forum3.Models.ViewModels;

	public class MigratorService {
		DataModels.ApplicationDbContext AppDb { get; }
		MigratorModels.MigratorDbContext LegacyDb { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IUrlHelper UrlHelper { get; }
		CloudBlobClient CloudBlobClient { get; }

		public MigratorService(
			DataModels.ApplicationDbContext applicationDbContext,
			MigratorModels.MigratorDbContext migratorDbContext,
			RoleManager<DataModels.ApplicationRole> roleManager,
			UserManager<DataModels.ApplicationUser> userManager,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory,
			CloudBlobClient cloudBlobClient
		) {
			AppDb = applicationDbContext;
			LegacyDb = migratorDbContext;
			RoleManager = roleManager;
			UserManager = userManager;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
			CloudBlobClient = cloudBlobClient;
		}

		public async Task<ViewModels.Delay> Execute(InputModels.Continue input) {
			var viewModel = new ViewModels.Delay();

			if (input == null || string.IsNullOrEmpty(input.Stage))
				input = new InputModels.Continue {
					Stage = nameof(MigrateUsers),
					CurrentStep = 0
				};

			var nextStage = string.Empty;

			switch (input.Stage) {
				case nameof(MigrateUsers):
					await MigrateUsers(input);
					nextStage = nameof(MigrateRoles);
					break;

				case nameof(MigrateRoles):
					await MigrateRoles(input);
					nextStage = nameof(MigrateBoards);
					break;

				case nameof(MigrateBoards):
					await MigrateBoards(input);
					nextStage = nameof(MigrateMessages);
					break;

				case nameof(MigrateMessages):
					MigrateMessages(input);
					nextStage = nameof(MigratePins);
					break;

				case nameof(MigratePins):
					await MigratePins(input);
					nextStage = nameof(MigrateMessageBoards);
					break;

				case nameof(MigrateMessageBoards):
					await MigrateMessageBoards(input);
					nextStage = nameof(MigrateSmileys);
					break;

				case nameof(MigrateSmileys):
					await MigrateSmileys(input);
					nextStage = nameof(MigrateMessageThoughts);
					break;

				case nameof(MigrateMessageThoughts):
					await MigrateMessageThoughts(input);
					nextStage = nameof(MigrateViewLogs);
					break;

				case nameof(MigrateViewLogs):
					await MigrateViewLogs(input);
					nextStage = nameof(RemoveInviteOnlyTopics);
					break;

				case nameof(RemoveInviteOnlyTopics):
					await RemoveInviteOnlyTopics(input);
					nextStage = string.Empty;
					break;

				default:
					throw new ArgumentException(nameof(input.Stage));
			}

			viewModel.ActionName = input.Stage;
			viewModel.CurrentPage = input.CurrentStep;
			viewModel.TotalPages = input.TotalSteps;

			if (input.CurrentStep == input.TotalSteps) {
				input.Stage = nextStage;
				input.TotalSteps = 0;
				input.CurrentStep = 0;
			}
			else
				input.CurrentStep++;

			if (string.IsNullOrEmpty(input.Stage))
				viewModel.NextAction = UrlHelper.Action(nameof(Boards.Index), nameof(Boards));
			else
				viewModel.NextAction = UrlHelper.Action(nameof(Migrator.Run), nameof(Migrator), input);

			return viewModel;
		}

		async Task MigrateUsers(InputModels.Continue input) {
			if (await AppDb.Messages.AnyAsync())
				return;

			var take = 1;

			if (input.CurrentStep == 0) {
				var legacyRecordCount = await LegacyDb.UserProfiles.CountAsync();
				input.TotalSteps = Convert.ToInt32(Math.Ceiling(1D * legacyRecordCount / take));
				return;
			}

			var query = from user in LegacyDb.UserProfiles
						join membership in LegacyDb.Membership on user.UserId equals membership.UserId
						select new DataModels.ApplicationUser {
							LegacyId = user.UserId,
							PasswordHash = membership.Password,
							DisplayName = user.DisplayName,
							Birthday = user.Birthday,
							Email = user.UserName,
							EmailConfirmed = true,
							NormalizedEmail = user.UserName.ToUpper(),
							NormalizedUserName = user.UserName.ToUpper(),
							UserName = user.UserName,
							Registered = user.Registered,
							LastOnline = user.LastOnline,
						};

			var skip = take * (input.CurrentStep - 1);

			var records = query.Skip(skip).Take(take).ToList();

			foreach (var record in records)
				await UserManager.CreateAsync(record);
		}

		async Task MigrateRoles(InputModels.Continue input) {
			if (!await AppDb.Users.AnyAsync())
				return;

			if (await AppDb.Roles.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var legacyUsersInRoles = await LegacyDb.UsersInRoles.ToListAsync();
			var legacyRoles = await LegacyDb.Roles.ToListAsync();

			var query = from user in AppDb.Users
						join userInRole in legacyUsersInRoles on user.LegacyId equals userInRole.UserId
						join role in legacyRoles on userInRole.RoleId equals role.Id
						select new {
							User = user,
							RoleName = role.Name
						};

			var records = await query.ToListAsync();

			foreach (var record in records) {
				if (!await RoleManager.RoleExistsAsync(record.RoleName)) {
					await RoleManager.CreateAsync(new DataModels.ApplicationRole {
						Name = record.RoleName
					});
				}

				await UserManager.AddToRoleAsync(record.User, record.RoleName);
			}
		}

		async Task MigrateBoards(InputModels.Continue input) {
			if (await AppDb.Categories.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var category = new DataModels.Category {
				Name = "Migration Category",
				DisplayOrder = 100
			};

			AppDb.Add(category);

			await AppDb.SaveChangesAsync();

			var query = from board in LegacyDb.Boards
						select new DataModels.Board {
							LegacyId = board.Id,
							CategoryId = category.Id,
							DisplayOrder = board.DisplayOrder,
							Name = board.Name
						};

			var records = await query.ToListAsync();

			foreach (var record in records)
				AppDb.Add(record);

			await AppDb.SaveChangesAsync();
		}

		void MigrateMessages(InputModels.Continue input) {
			if (!AppDb.Users.Any())
				return;

			if (!AppDb.Boards.Any())
				return;

			if (input.CurrentStep == 0 && AppDb.Messages.Any())
				return;

			var take = 500;

			if (input.CurrentStep == 0) {
				var legacyRecordCount = LegacyDb.Messages.Count();

				input.TotalSteps = Convert.ToInt32(Math.Ceiling(1D * legacyRecordCount / take));
				input.CurrentStep = 1;
				return;
			}

			var query = from message in LegacyDb.Messages
						orderby message.TimePosted
						select message;

			var skip = take * (input.CurrentStep - 1);

			var records = query.Skip(skip).Take(take).ToList();

			var users = AppDb.Users.ToList();

			foreach (var record in records) {
				var newMessage = new DataModels.Message {
					Processed = false,
					OriginalBody = record.OriginalBody,
					ShortPreview = record.Subject,
					LongPreview = string.Empty,
					DisplayBody = string.Empty,
					TimePosted = record.TimePosted,
					TimeEdited = record.TimeEdited,
					LastReplyPosted = record.LastChildTimePosted,
					ReplyCount = record.Replies,
					ViewCount = record.Views,
					LegacyId = record.Id,
					LegacyParentId = record.ParentId,
					LegacyReplyId = record.ReplyId,
					LegacyPostedById = record.PostedById,
					LegacyEditedById = record.EditedById,
					LegacyLastReplyById = record.LastChildById,
				};

				newMessage.PostedById = users.SingleOrDefault(u => u.LegacyId == newMessage.LegacyPostedById)?.Id ?? string.Empty;
				newMessage.EditedById = users.SingleOrDefault(u => u.LegacyId == newMessage.LegacyEditedById)?.Id ?? string.Empty;
				newMessage.LastReplyById = users.SingleOrDefault(u => u.LegacyId == newMessage.LegacyLastReplyById)?.Id ?? string.Empty;

				AppDb.Add(newMessage);
				AppDb.SaveChanges();
			}
		}

		async Task MigrateMessageBoards(InputModels.Continue input) {
			if (!await AppDb.Users.AnyAsync())
				return;

			if (!await AppDb.Messages.AnyAsync())
				return;

			if (!await AppDb.Boards.AnyAsync())
				return;

			if (await AppDb.MessageBoards.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var legacyMessageBoards = await LegacyDb.MessageBoards.ToListAsync();

			var query = from message in AppDb.Messages
						where message.ParentId == 0
						join messageBoard in legacyMessageBoards on message.LegacyId equals messageBoard.MessageId
						join board in AppDb.Boards on messageBoard.BoardId equals board.LegacyId
						join user in AppDb.Users on messageBoard.UserId equals user.LegacyId
						select new DataModels.MessageBoard {
							MessageId = message.Id,
							BoardId = board.Id,
							UserId = user.Id,
							TimeAdded = messageBoard.TimeAdded
						};

			var records = await query.ToListAsync();

			foreach (var record in records)
				AppDb.Add(record);

			await AppDb.SaveChangesAsync();
		}

		async Task MigratePins(InputModels.Continue input) {
			if (!await AppDb.Users.AnyAsync())
				return;

			if (!await AppDb.Messages.AnyAsync())
				return;

			if (await AppDb.Pins.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var legacyPins = await LegacyDb.Pins.ToListAsync();

			var query = from user in AppDb.Users
						join pin in legacyPins on user.LegacyId equals pin.UserId
						join message in AppDb.Messages on pin.MessageId equals message.LegacyId
						select new DataModels.Pin {
							MessageId = message.Id,
							Time = pin.Time,
							UserId = user.Id
						};

			var records = await query.ToListAsync();

			foreach (var record in records)
				AppDb.Add(record);

			await AppDb.SaveChangesAsync();
		}

		async Task MigrateSmileys(InputModels.Continue input) {
			if (!await AppDb.Users.AnyAsync())
				return;

			if (!await AppDb.Messages.AnyAsync())
				return;

			if (await AppDb.Smileys.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var legacySmileys = await LegacyDb.Smileys.ToListAsync();

			foreach (var record in legacySmileys) {
				var column = Math.Floor(record.DisplayOrder);
				var row = record.DisplayOrder - column;

				var newSmiley = new DataModels.Smiley {
					Code = record.Code,
					FileName = record.Path,
					Thought = record.Thought,
					SortOrder = Convert.ToInt32((1000 * column) + (100 * row)),
				};

				var container = CloudBlobClient.GetContainerReference("smileys");

				if (await container.CreateIfNotExistsAsync())
					await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

				var blobReference = container.GetBlockBlobReference(record.Path);

				// Multiple smileys can point to the same image.
				if (!await blobReference.ExistsAsync()) {
					blobReference.Properties.ContentType = "image/gif";

					using (var fileStream = new FileStream($"wwwroot/images/Smileys/{record.Path}", FileMode.Open, FileAccess.Read, FileShare.Read)) {
						fileStream.Position = 0;
						await blobReference.UploadFromStreamAsync(fileStream);
					}
				}

				newSmiley.Path = blobReference.Uri.AbsoluteUri;

				AppDb.Add(newSmiley);
			}

			await AppDb.SaveChangesAsync();
		}

		async Task MigrateMessageThoughts(InputModels.Continue input) {
			if (!await AppDb.Users.AnyAsync())
				return;

			if (!await AppDb.Messages.AnyAsync())
				return;

			if (!await AppDb.Smileys.AnyAsync())
				return;

			if (await AppDb.MessageThoughts.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var legacyThoughts = await LegacyDb.MessageThoughts.ToListAsync();
			var legacySmileys = await LegacyDb.Smileys.ToListAsync();

			var query = from user in AppDb.Users
						join thought in legacyThoughts on user.LegacyId equals thought.UserId
						join message in AppDb.Messages on thought.MessageId equals message.LegacyId
						join legacySmiley in legacySmileys on thought.SmileyId equals legacySmiley.Id
						join smiley in AppDb.Smileys on legacySmiley.Code equals smiley.Code
						select new DataModels.MessageThought {
							MessageId = message.Id,
							SmileyId = smiley.Id,
							UserId = user.Id
						};

			var records = await query.ToListAsync();

			foreach (var record in records)
				AppDb.Add(record);

			await AppDb.SaveChangesAsync();
		}

		async Task MigrateViewLogs(InputModels.Continue input) {
			if (!await AppDb.Users.AnyAsync())
				return;

			if (!await AppDb.Messages.AnyAsync())
				return;

			if (input.CurrentStep == 0 && await AppDb.ViewLogs.AnyAsync())
				return;

			var take = 100;

			var historyTimeLimit = DateTime.Now.AddDays(-14);
			var userRecords = await AppDb.Users.Select(u => new { u.Id, u.LegacyId }).ToListAsync();

			if (input.CurrentStep == 0) {
				var legacyRecordCount = LegacyDb.ViewLogs.Where(r => r.LogTime >= historyTimeLimit).Count();

				var newRecords = new List<DataModels.ViewLog>();

				foreach (var user in userRecords) {
					newRecords.Add(new DataModels.ViewLog {
						LogTime = historyTimeLimit,
						TargetType = Enums.EViewLogTargetType.All,
						UserId = user.Id
					});
				}

				AppDb.ViewLogs.AddRange(newRecords);
				AppDb.SaveChanges();

				input.TotalSteps = Convert.ToInt32(Math.Ceiling(1D * legacyRecordCount / take));
				input.CurrentStep = 1;
				return;
			}

			var query = from record in LegacyDb.ViewLogs
						where record.LogTime >= historyTimeLimit
						select record;

			var skip = take * (input.CurrentStep - 1);

			var legacyRecords = query.Skip(skip).Take(take).ToList();

			foreach (var legacyRecord in legacyRecords) {
				var user = userRecords.FirstOrDefault(r => r.LegacyId == legacyRecord.UserId);

				if (user is null)
					continue;

				switch (legacyRecord.TargetType) {
					case MigratorModels.OldViewLogTargetType.User:
						var allViewLog = AppDb.ViewLogs.Where(r => r.UserId == user.Id)?.FirstOrDefault(r => r.TargetType == Enums.EViewLogTargetType.All);

						if (allViewLog != null && allViewLog.LogTime < legacyRecord.LogTime) {
							allViewLog.LogTime = legacyRecord.LogTime;
							AppDb.Update(allViewLog);
						}

						break;

					case MigratorModels.OldViewLogTargetType.Message:
						var messageViewLog = AppDb.ViewLogs.Where(r => r.UserId == user.Id && r.TargetType == Enums.EViewLogTargetType.Message && r.TargetId == legacyRecord.TargetId).FirstOrDefault();

						if (messageViewLog is null) {
							var messageRecordId = AppDb.Messages.Where(r => r.LegacyId == legacyRecord.TargetId).Select(r => r.Id).FirstOrDefault();

							if (messageRecordId > 0) {
								AppDb.ViewLogs.Add(new DataModels.ViewLog {
									UserId = user.Id,
									TargetType = Enums.EViewLogTargetType.Message,
									LogTime = legacyRecord.LogTime,
									TargetId = messageRecordId
								});
							}
						}
						else {
							messageViewLog.LogTime = legacyRecord.LogTime;
							AppDb.Update(messageViewLog);
						}

						break;
				}
			}

			AppDb.SaveChanges();
		}

		async Task RemoveInviteOnlyTopics(InputModels.Continue input) {
			if (!await AppDb.Messages.AnyAsync())
				return;

			if (!await AppDb.Pins.AnyAsync())
				return;

			if (!await AppDb.MessageBoards.AnyAsync())
				return;

			if (!await AppDb.MessageThoughts.AnyAsync())
				return;

			input.TotalSteps = 1;
			input.CurrentStep = 1;

			var legacyInviteOnlyTopics = await LegacyDb.InviteOnlyTopicUsers.Select(r => r.MessageId).Distinct().ToListAsync();
			var messages = await AppDb.Messages.Where(m => legacyInviteOnlyTopics.Contains(m.LegacyId) || legacyInviteOnlyTopics.Contains(m.LegacyParentId)).ToListAsync();
			var messageIds = messages.Select(m => m.Id).Distinct();

			foreach (var messageId in messageIds) {
				var pinsTask = AppDb.Pins.Where(r => r.MessageId == messageId).ToListAsync();
				var messageBoardsTask = AppDb.MessageBoards.Where(r => r.MessageId == messageId).ToListAsync();
				var messageThoughtsTask = AppDb.MessageThoughts.Where(r => r.MessageId == messageId).ToListAsync();

				Task.WaitAll(pinsTask, messageBoardsTask, messageThoughtsTask);

				AppDb.Pins.RemoveRange(pinsTask.Result);
				AppDb.MessageBoards.RemoveRange(messageBoardsTask.Result);
				AppDb.MessageThoughts.RemoveRange(messageThoughtsTask.Result);
			}

			AppDb.Messages.RemoveRange(messages);
			await AppDb.SaveChangesAsync();
		}
	}
}