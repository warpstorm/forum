using Forum.Core.Models.Errors;
using Forum.Data.Contexts;
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
	using DataModels = Data.Models;
	using InputModels = Models.InputModels;

	public class Administration : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		MessageRepository MessageRepository { get; }
		AccountRepository AccountRepository { get; }
		RoleRepository RoleRepository { get; }
		TopicRepository TopicRepository { get; }

		public Administration(
			ApplicationDbContext dbContext,
			UserContext userContext,
			MessageRepository messageRepository,
			RoleRepository roleRepository,
			AccountRepository accountRepository,
			TopicRepository topicRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			MessageRepository = messageRepository;
			RoleRepository = roleRepository;
			AccountRepository = accountRepository;
			TopicRepository = topicRepository;
		}

		[HttpGet]
		public IActionResult Install() {
			CheckInstallContext();

			return View("Process", new List<string> {
				Url.Action(nameof(InstallRoles)),
				Url.Action(nameof(InstallCategories)),
				Url.Action(nameof(InstallBoards)),
				Url.Action(nameof(InstallAdmins)),
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

			var roles = await RoleRepository.SiteRoles();

			if (roles.Any()) {
				return Ok("Roles already exist in the database. Not going to create new ones.");
			}

			await RoleRepository.Create(new InputModels.CreateRoleInput {
				Name = Constants.InternalKeys.Admin,
				Description = "Forum administrators"
			});

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
				return Ok("You are already an administrator.");
			}

			var adminRole = (await RoleRepository.SiteRoles()).First(r => r.Name == Constants.InternalKeys.Admin);
			await RoleRepository.AddUser(adminRole.Id, UserContext.ApplicationUser.Id);

			await AccountRepository.SignOut();

			return Ok("Forum install is complete. Please click the title image to continue.");
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
				return Ok("Categories already exist. Not going to create new categories.");
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
				return Ok("Boards already exist. Not going to create new ones.");
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
