using System;
using System.Linq;
using System.Threading.Tasks;
using Forum3.Controllers;
using Forum3.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

using DataModels = Forum3.Models.DataModels;
using InputModels = Forum3.Models.InputModels;
using MigratorModels = Forum3.Migrator.Models;
using ViewModels = Forum3.Models.ViewModels;

namespace Forum3.Migrator {
	public class MigratorService {
		DataModels.ApplicationDbContext AppDb { get; }
		MigratorModels.MigratorDbContext LegacyDb { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IUrlHelper UrlHelper { get; }

		public MigratorService(
			DataModels.ApplicationDbContext applicationDbContext,
			MigratorModels.MigratorDbContext migratorDbContext,
			RoleManager<DataModels.ApplicationRole> roleManager,
			UserManager<DataModels.ApplicationUser> userManager,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			AppDb = applicationDbContext;
			LegacyDb = migratorDbContext;
			RoleManager = roleManager;
			UserManager = userManager;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<bool> ConnectionTest() => await LegacyDb.Messages.AnyAsync();

		public async Task<ViewModels.Delay> Execute(InputModels.Continue input) {
			var routeValueModel = new InputModels.Continue();
			var viewModel = new ViewModels.Delay();

			var stage = input?.Stage ?? nameof(MigrateUsers);
			var step = input?.Step ?? 0;

			var nextStage = string.Empty;

			var result = new MigrationResult(0, 0);

			switch (stage) {
				case nameof(MigrateUsers):
					result = await MigrateUsers(step);
					nextStage = nameof(MigrateRoles);
					break;

				case nameof(MigrateRoles):
					result = await MigrateRoles(step);
					nextStage = nameof(MigrateBoards);
					break;

				case nameof(MigrateBoards):
					result = await MigrateBoards(step);
					nextStage = nameof(MigrateMessages);
					break;

				case nameof(MigrateMessages):
					result = await MigrateMessages(step);
					nextStage = nameof(MigratePins);
					break;

				case nameof(MigratePins):
					result = await MigratePins(step);
					nextStage = nameof(MigrateSmileys);
					break;

				case nameof(MigrateSmileys):
					result = await MigrateSmileys(step);
					nextStage = nameof(MigrateMessageThoughts);
					break;

				case nameof(MigrateMessageThoughts):
					result = await MigrateMessageThoughts(step);
					nextStage = nameof(MigrateMessageBoards);
					break;

				case nameof(MigrateMessageBoards):
					result = await MigrateMessageBoards(step);
					nextStage = string.Empty;
					break;

				default:
					throw new ArgumentException(nameof(input.Stage));
			}

			if (result.Next < result.Total) {
				routeValueModel.Stage = stage;
				routeValueModel.Step = result.Next;
			}
			else {
				routeValueModel.Stage = nextStage;
				routeValueModel.Step = 0;
			}

			viewModel.CurrentPage = step;
			viewModel.TotalPages = result.Total;

			if (string.IsNullOrEmpty(routeValueModel.Stage))
				viewModel.NextAction = UrlHelper.Action(nameof(Boards.Index), nameof(Boards));
			else
				viewModel.NextAction = UrlHelper.Action(nameof(Migrator.Run), nameof(Migrator), routeValueModel);

			return viewModel;
		}

		async Task<MigrationResult> MigrateUsers(int current) {
			var result = new MigrationResult(0, 0);

			if (await AppDb.Users.AnyAsync())
				return result;

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

			var records = await query.ToListAsync();

			foreach (var record in records)
				await UserManager.CreateAsync(record);

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		async Task<MigrationResult> MigrateRoles(int current) {
			AppDb.Users.ThrowIfEmpty(nameof(AppDb.Users));

			var result = new MigrationResult(0, 0);

			if (await AppDb.Roles.AnyAsync())
				return result;

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

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		async Task<MigrationResult> MigrateBoards(int current) {
			var result = new MigrationResult(0, 0);

			if (await AppDb.Categories.AnyAsync())
				return result;

			var category = new DataModels.Category {
				Name = "Migration Category",
				DisplayOrder = 100
			};

			await AppDb.AddAsync(category);
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
				await AppDb.AddAsync(record);

			await AppDb.SaveChangesAsync();

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		async Task<MigrationResult> MigrateMessages(int current) {
			AppDb.Users.ThrowIfEmpty(nameof(AppDb.Users));
			AppDb.Boards.ThrowIfEmpty(nameof(AppDb.Boards));

			var result = new MigrationResult(0, 0);

			if (await AppDb.Messages.AnyAsync())
				return result;

			var take = 100;

			if (current == 0) {
				var legacyMessageCount = await LegacyDb.Messages.CountAsync();

				result.Total = Convert.ToInt32(Math.Ceiling(1D * legacyMessageCount / take));
				result.Next = 1;
			}
			else {
				var query = from message in LegacyDb.Messages
							select new DataModels.Message {
								Processed = false,
								OriginalBody = message.OriginalBody,
								ShortPreview = message.Subject,
								TimePosted = message.TimePosted,
								TimeEdited = message.TimeEdited,
								LastReplyPosted = message.LastChildTimePosted,
								ReplyCount = message.Replies,
								ViewCount = message.Views,
								LegacyId = message.Id,
								LegacyParentId = message.ParentId,
								LegacyReplyId = message.ReplyId,
								LegacyPostedById = message.PostedById,
								LegacyEditedById = message.EditedById,
								LegacyLastReplyById = message.LastChildById,
							};

				var skip = take * (current - 1);

				var records = await query.Skip(skip).Take(take).ToListAsync();
				var users = await AppDb.Users.ToListAsync();

				foreach (var record in records) {
					record.PostedById = users.Single(u => u.LegacyId == record.LegacyPostedById).Id;
					record.EditedById = users.Single(u => u.LegacyId == record.LegacyEditedById).Id;
					record.LastReplyById = users.Single(u => u.LegacyId == record.LegacyLastReplyById).Id;

					await AppDb.AddAsync(record);
				}
			}

			return result;
		}

		async Task<MigrationResult> MigrateMessageThoughts(int current) {
			AppDb.Users.ThrowIfEmpty(nameof(AppDb.Users));
			AppDb.Messages.ThrowIfEmpty(nameof(AppDb.Messages));
			AppDb.Smileys.ThrowIfEmpty(nameof(AppDb.Smileys));

			var result = new MigrationResult(0, 0);

			if (await AppDb.MessageThoughts.AnyAsync())
				return result;

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
				await AppDb.AddAsync(record);

			await AppDb.SaveChangesAsync();

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		async Task<MigrationResult> MigrateMessageBoards(int current) {
			AppDb.Users.ThrowIfEmpty(nameof(AppDb.Users));
			AppDb.Messages.ThrowIfEmpty(nameof(AppDb.Messages));
			AppDb.Boards.ThrowIfEmpty(nameof(AppDb.Boards));

			var result = new MigrationResult(0, 0);

			if (await AppDb.MessageBoards.AnyAsync())
				return result;

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
				await AppDb.AddAsync(record);

			await AppDb.SaveChangesAsync();

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		async Task<MigrationResult> MigratePins(int current) {
			AppDb.Users.ThrowIfEmpty(nameof(AppDb.Users));
			AppDb.Messages.ThrowIfEmpty(nameof(AppDb.Messages));

			var result = new MigrationResult(0, 0);

			if (await AppDb.MessageBoards.AnyAsync())
				return result;

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
				await AppDb.AddAsync(record);

			await AppDb.SaveChangesAsync();

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		// Unfinished. Needs image uploading.
		async Task<MigrationResult> MigrateSmileys(int current) {
			AppDb.Users.ThrowIfEmpty(nameof(AppDb.Users));
			AppDb.Messages.ThrowIfEmpty(nameof(AppDb.Messages));

			var result = new MigrationResult(0, 0);

			if (await AppDb.MessageBoards.AnyAsync())
				return result;

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

				await AppDb.AddAsync(newSmiley);
			}

			await AppDb.SaveChangesAsync();

			result.Total = 1;
			result.Next = 1;

			return result;
		}

		class MigrationResult {
			public int Total { get; set; }
			public int Next { get; set; }

			public MigrationResult(int total, int next) {
				Total = total;
				Next = next;
			}
		}
	}
}