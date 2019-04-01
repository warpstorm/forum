using Forum.Models;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemViewModels = Models.ViewModels.Boards.Items;
	using ServiceModels = Models.ServiceModels;

	public class BoardRepository : IRepository<DataModels.Board> {
		public async Task<List<DataModels.Board>> Records() {
			if (_Records is null) {
				_Records = await DbContext.Boards.ToListAsync();
				_Records = _Records.OrderBy(r => r.DisplayOrder).ToList();
			}

			return _Records;
		}
		List<DataModels.Board> _Records;

		public async Task<List<DataModels.Category>> Categories() {
			if (_Categories is null) {
				var records = await DbContext.Categories.ToListAsync();
				_Categories = records.OrderBy(record => record.DisplayOrder).ToList();
			}

			return _Categories;
		}
		List<DataModels.Category> _Categories;

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		RoleRepository RoleRepository { get; }
		IUrlHelper UrlHelper { get; }

		public BoardRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			RoleRepository roleRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			RoleRepository = roleRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<List<SelectListItem>> CategoryPickList() {
			var pickList = new List<SelectListItem>();

			foreach (var categoryRecord in await Categories()) {
				pickList.Add(new SelectListItem {
					Text = categoryRecord.Name,
					Value = categoryRecord.Id.ToString()
				});
			}

			return pickList;
		}

		public async Task<List<ItemViewModels.IndexCategory>> CategoryIndex(bool includeReplies = false) {
			var categories = (await Categories()).OrderBy(r => r.DisplayOrder).ToList();

			var indexCategories = new List<ItemViewModels.IndexCategory>();

			foreach (var categoryRecord in categories) {
				var indexCategory = new ItemViewModels.IndexCategory {
					Id = categoryRecord.Id.ToString(),
					Name = categoryRecord.Name,
					DisplayOrder = categoryRecord.DisplayOrder
				};

				foreach (var board in (await Records()).Where(r => r.CategoryId == categoryRecord.Id)) {
					var thisBoardRoles = from role in await RoleRepository.BoardRoles()
										 where role.BoardId == board.Id
										 select role;

					var authorized = UserContext.IsAdmin || !thisBoardRoles.Any() || (UserContext.Roles?.Any(userRole => thisBoardRoles.Any(boardRole => boardRole.RoleId == userRole)) ?? false);

					if (!authorized) {
						continue;
					}

					var indexBoard = await GetIndexBoard(board, includeReplies);

					indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any()) {
					indexCategories.Add(indexCategory);
				}
			}

			return indexCategories;
		}

		public async Task<ItemViewModels.IndexBoard> GetIndexBoard(DataModels.Board boardRecord, bool includeReplies = false) {
			var indexBoard = new ItemViewModels.IndexBoard {
				Id = boardRecord.Id.ToString(),
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				DisplayOrder = boardRecord.DisplayOrder,
				Unread = false
			};

			if (includeReplies) {
				var messages = from topicBoard in DbContext.TopicBoards
							   join message in DbContext.Messages on topicBoard.MessageId equals message.Id
							   where topicBoard.BoardId == boardRecord.Id
							   where !message.Deleted
							   orderby message.LastReplyPosted descending
							   select new {
								   topicBoard.MessageId,
								   LastReplyId = message.LastReplyId > 0 ? message.LastReplyId : message.Id,
								   TopicPreview = message.ShortPreview
							   };

				var users = await AccountRepository.Records();

				// Only checks the most recent 10 topics. If all 10 are forbidden, then LastMessage stays null.
				foreach (var item in messages.Take(10)) {
					var topicBoardIdsQuery = from topicBoard in DbContext.TopicBoards
											 where topicBoard.MessageId == item.MessageId
											 select topicBoard.BoardId;

					var topicBoardIds = topicBoardIdsQuery.ToList();

					var relevantRoleIds = from role in await RoleRepository.BoardRoles()
										  where topicBoardIds.Contains(role.BoardId)
										  select role.RoleId;

					if (UserContext.IsAdmin || !relevantRoleIds.Any() || relevantRoleIds.Intersect(UserContext.Roles).Any()) {
						var lastReplyQuery = from message in DbContext.Messages
											 where message.Id == item.LastReplyId
											 where !message.Deleted
											 select new Models.ViewModels.Topics.Items.TopicPreview {
												 Id = message.Id,
												 FirstMessageShortPreview = item.TopicPreview,
												 LastMessageId = message.LastReplyId,
												 LastMessagePostedById = message.LastReplyById,
												 LastMessageTimePosted = message.LastReplyPosted,
											 };

						indexBoard.RecentTopic = lastReplyQuery.FirstOrDefault();
						indexBoard.RecentTopic.LastMessagePostedByName = users.FirstOrDefault(r => r.Id == indexBoard.RecentTopic.LastMessagePostedById)?.DecoratedName ?? "User";
						break;
					}
				}
			}

			return indexBoard;
		}

		public async Task<ServiceModels.ServiceResponse> AddBoard(InputModels.CreateBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if ((await Records()).Any(b => b.Name == input.Name)) {
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");
			}

			DataModels.Category categoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				input.NewCategory = input.NewCategory.Trim();
			}

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				categoryRecord = (await Categories()).FirstOrDefault(c => c.Name == input.NewCategory);

				if (categoryRecord is null) {
					var displayOrder = (await Categories()).DefaultIfEmpty().Max(c => c.DisplayOrder);

					categoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					DbContext.Categories.Add(categoryRecord);
				}
			}
			else {
				try {
					var categoryId = Convert.ToInt32(input.Category);
					categoryRecord = (await Categories()).First(c => c.Id == categoryId);

					if (categoryRecord is null) {
						serviceResponse.Error(nameof(input.Category), "No category was found with this ID.");
					}
				}
				catch (FormatException) {
					serviceResponse.Error(nameof(input.Category), "Invalid category ID");
				}
			}

			if (!string.IsNullOrEmpty(input.Name)) {
				input.Name = input.Name.Trim();
			}

			if (string.IsNullOrEmpty(input.Name)) {
				serviceResponse.Error(nameof(input.Name), "Name is a required field.");
			}

			if (!string.IsNullOrEmpty(input.Description)) {
				input.Description = input.Description.Trim();
			}

			var existingRecord = (await Records()).FirstOrDefault(b => b.Name == input.Name);

			if (existingRecord != null) {
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			DbContext.SaveChanges();

			var record = new DataModels.Board {
				Name = input.Name,
				Description = input.Description,
				CategoryId = categoryRecord.Id
			};

			DbContext.Boards.Add(record);

			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Boards.Manage), nameof(Controllers.Boards), new { id = record.Id });

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> UpdateBoard(InputModels.EditBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = (await Records()).FirstOrDefault(b => b.Id == input.Id);

			if (record is null) {
				serviceResponse.Error($"A record does not exist with ID '{input.Id}'");
			}

			DataModels.Category newCategoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				input.NewCategory = input.NewCategory.Trim();
			}

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				newCategoryRecord = (await Categories()).FirstOrDefault(c => c.Name == input.NewCategory);

				if (newCategoryRecord is null) {
					var displayOrder = (await Categories()).DefaultIfEmpty().Max(c => c.DisplayOrder);

					newCategoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					DbContext.Categories.Add(newCategoryRecord);
					DbContext.SaveChanges();
				}
			}
			else {
				try {
					var newCategoryId = Convert.ToInt32(input.Category);
					newCategoryRecord = (await Categories()).FirstOrDefault(c => c.Id == newCategoryId);

					if (newCategoryRecord is null) {
						serviceResponse.Error(nameof(input.Category), "No category was found with this ID.");
					}
				}
				catch (FormatException) {
					serviceResponse.Error(nameof(input.Category), "Invalid category ID");
				}
			}

			if (!string.IsNullOrEmpty(input.Name)) {
				input.Name = input.Name.Trim();
			}

			if (string.IsNullOrEmpty(input.Name)) {
				serviceResponse.Error(nameof(input.Name), "Name is a required field.");
			}

			if (!string.IsNullOrEmpty(input.Description)) {
				input.Description = input.Description.Trim();
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			record.Name = input.Name;
			record.Description = input.Description;

			var oldCategoryId = -1;

			if (record.CategoryId != newCategoryRecord.Id) {
				var categoryBoards = (await Records()).Where(r => r.CategoryId == record.CategoryId).ToList();

				if (categoryBoards.Count() <= 1) {
					oldCategoryId = record.CategoryId;
				}

				record.CategoryId = newCategoryRecord.Id;
			}

			var boardRoles = (from role in await RoleRepository.BoardRoles()
							  where role.BoardId == record.Id
							  select role).ToList();

			foreach (var boardRole in boardRoles) {
				DbContext.BoardRoles.Remove(boardRole);
			}

			if (input.Roles != null) {
				var roleIds = (from role in await RoleRepository.SiteRoles()
							   select role.Id).ToList();

				foreach (var inputRole in input.Roles) {
					if (roleIds.Contains(inputRole)) {
						DbContext.BoardRoles.Add(new DataModels.BoardRole {
							BoardId = record.Id,
							RoleId = inputRole
						});
					}
					else {
						serviceResponse.Error($"Role does not exist with id '{inputRole}'");
					}
				}
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			DbContext.Update(record);
			DbContext.SaveChanges();

			if (oldCategoryId >= 0) {
				var oldCategoryRecord = (await Categories()).FirstOrDefault(item => item.Id == oldCategoryId);

				if (oldCategoryRecord != null) {
					DbContext.Categories.Remove(oldCategoryRecord);
					DbContext.SaveChanges();
				}
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Boards.Manage), nameof(Controllers.Boards), new { id = record.Id });

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MergeBoard(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromBoard = (await Records()).FirstOrDefault(b => b.Id == input.FromId);
			var toBoard = (await Records()).FirstOrDefault(b => b.Id == input.ToId);

			if (fromBoard is null) {
				serviceResponse.Error($"A record does not exist with ID '{input.FromId}'");
			}

			if (toBoard is null) {
				serviceResponse.Error($"A record does not exist with ID '{input.ToId}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var topicBoards = DbContext.TopicBoards.Where(m => m.BoardId == fromBoard.Id).ToList();

			// Reassign messages to new board
			foreach (var topicBoard in topicBoards) {
				topicBoard.BoardId = toBoard.Id;
				DbContext.Update(topicBoard);
			}

			DbContext.SaveChanges();

			var categoryId = fromBoard.CategoryId;

			// Delete the board
			DbContext.Boards.Remove(fromBoard);

			DbContext.SaveChanges();

			// Remove the category if empty
			if (!DbContext.Boards.Any(b => b.CategoryId == categoryId)) {
				var categoryRecord = (await Categories()).FirstOrDefault(item => item.Id == categoryId);

				if (categoryRecord != null) {
					DbContext.Categories.Remove(categoryRecord);
					DbContext.SaveChanges();
				}
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MergeCategory(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromCategory = (await Categories()).FirstOrDefault(b => b.Id == input.FromId);
			var toCategory = (await Categories()).FirstOrDefault(b => b.Id == input.ToId);

			if (fromCategory is null) {
				serviceResponse.Error($"A record does not exist with ID '{input.FromId}'");
			}

			if (toCategory is null) {
				serviceResponse.Error($"A record does not exist with ID '{input.ToId}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var displacedBoards = (await Records()).Where(b => b.CategoryId == fromCategory.Id).ToList();

			foreach (var displacedBoard in displacedBoards) {
				displacedBoard.CategoryId = toCategory.Id;
				DbContext.Update(displacedBoard);
			}

			DbContext.SaveChanges();

			DbContext.Categories.Remove(fromCategory);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MoveBoardUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetBoard = (await Records()).FirstOrDefault(b => b.Id == id);

			if (targetBoard is null) {
				serviceResponse.Error("No board found with that ID.");
				return serviceResponse;
			}

			var categoryBoards = (await Records()).Where(b => b.CategoryId == targetBoard.CategoryId).OrderBy(b => b.DisplayOrder).ToList();

			var currentIndex = 1;

			foreach (var board in categoryBoards) {
				board.DisplayOrder = currentIndex++;
				DbContext.Update(board);
			}

			DbContext.SaveChanges();

			targetBoard = categoryBoards.First(b => b.Id == id);

			if (targetBoard.DisplayOrder > 1) {
				var displacedBoard = categoryBoards.FirstOrDefault(b => b.DisplayOrder == targetBoard.DisplayOrder - 1);

				if (displacedBoard != null) {
					displacedBoard.DisplayOrder++;
					DbContext.Update(displacedBoard);
				}

				targetBoard.DisplayOrder--;
				DbContext.Update(targetBoard);

				DbContext.SaveChanges();
			}
			else {
				targetBoard.DisplayOrder = 2;
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MoveCategoryUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetCategory = (await Categories()).FirstOrDefault(b => b.Id == id);

			if (targetCategory is null) {
				serviceResponse.Error("No category found with that ID.");
				return serviceResponse;
			}

			if (targetCategory.DisplayOrder > 1) {
				var displacedCategory = (await Categories()).First(b => b.DisplayOrder == targetCategory.DisplayOrder - 1);

				displacedCategory.DisplayOrder++;
				DbContext.Update(displacedCategory);

				targetCategory.DisplayOrder--;
				DbContext.Update(targetCategory);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}

		public async Task<bool> CanAccess(int topicId) {
			if (UserContext.IsAdmin) {
				return true;
			}

			var forbiddenBoardIdsQuery = from role in await RoleRepository.SiteRoles()
										 join board in await RoleRepository.BoardRoles() on role.Id equals board.RoleId
										 where !UserContext.Roles.Contains(role.Id)
										 select board.BoardId;

			var forbiddenBoardIds = forbiddenBoardIdsQuery.ToList();

			var topicBoards = await DbContext.TopicBoards.Where(item => item.TopicId == topicId).Select(item => item.BoardId).ToListAsync();

			return !topicBoards.Any() || !topicBoards.Intersect(forbiddenBoardIds).Any();
		}
	}
}