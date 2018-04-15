using Forum3.Contexts;
using Forum3.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemViewModels = Models.ViewModels.Boards.Items;
	using ServiceModels = Models.ServiceModels;

	public class BoardRepository : Repository<DataModels.Board> {
		ApplicationDbContext DbContext { get; }
		RoleRepository RoleRepository { get; }
		UserRepository UserRepository { get; }
		IUrlHelper UrlHelper { get; }

		public BoardRepository(
			ApplicationDbContext dbContext,
			RoleRepository roleRepository,
			UserRepository userRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			RoleRepository = roleRepository;
			UserRepository = userRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

			Records = DbContext.Boards.OrderBy(record => record.DisplayOrder).ToList();
		}

		public ItemViewModels.IndexBoard GetIndexItem(DataModels.Board boardRecord) {
			var indexBoard = new ItemViewModels.IndexBoard {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				DisplayOrder = boardRecord.DisplayOrder,
				Unread = false
			};

			if (boardRecord.LastMessageId != null) {
				// TODO - permission trim this list and handle empty shortPreviews

				var lastMessageQuery = from lastReply in DbContext.Messages
									   where lastReply.Id == boardRecord.LastMessageId
									   join lastReplyBy in UserRepository.All on lastReply.LastReplyById equals lastReplyBy.Id
									   select new Models.ViewModels.Topics.Items.MessagePreview {
										   Id = lastReply.Id,
										   ShortPreview = lastReply.ShortPreview,
										   LastReplyByName = lastReplyBy.DisplayName,
										   LastReplyId = lastReply.LastReplyId,
										   LastReplyPosted = lastReply.LastReplyPosted.ToPassedTimeString(),
										   LastReplyPreview = lastReply.ShortPreview
									   };

				indexBoard.LastMessage = lastMessageQuery.FirstOrDefault();
			}

			return indexBoard;
		}

		public ServiceModels.ServiceResponse Add(InputModels.CreateBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (Records.Any(b => b.Name == input.Name))
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			DataModels.Category categoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (categoryRecord is null) {
					var displayOrder = DbContext.Categories.DefaultIfEmpty().Max(c => c.DisplayOrder);

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
					categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == categoryId);

					if (categoryRecord is null)
						serviceResponse.Error(nameof(input.Category), "No category was found with this ID.");
				}
				catch (FormatException) {
					serviceResponse.Error(nameof(input.Category), "Invalid category ID");
				}
			}

			if (!string.IsNullOrEmpty(input.Name))
				input.Name = input.Name.Trim();

			if (string.IsNullOrEmpty(input.Name))
				serviceResponse.Error(nameof(input.Name), "Name is a required field.");

			if (!string.IsNullOrEmpty(input.Description))
				input.Description = input.Description.Trim();

			var existingRecord = Records.FirstOrDefault(b => b.Name == input.Name);

			if (existingRecord != null)
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			if (!serviceResponse.Success)
				return serviceResponse;

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

		public ServiceModels.ServiceResponse Update(InputModels.EditBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = Records.FirstOrDefault(b => b.Id == input.Id);

			if (record is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.Id}'");

			DataModels.Category newCategoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				newCategoryRecord = DbContext.Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (newCategoryRecord is null) {
					var displayOrder = DbContext.Categories.Max(c => c.DisplayOrder);

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
					newCategoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == newCategoryId);

					if (newCategoryRecord is null)
						serviceResponse.Error(nameof(input.Category), "No category was found with this ID.");
				}
				catch (FormatException) {
					serviceResponse.Error(nameof(input.Category), "Invalid category ID");
				}
			}

			if (!string.IsNullOrEmpty(input.Name))
				input.Name = input.Name.Trim();

			if (string.IsNullOrEmpty(input.Name))
				serviceResponse.Error(nameof(input.Name), "Name is a required field.");

			if (!string.IsNullOrEmpty(input.Description))
				input.Description = input.Description.Trim();

			if (!serviceResponse.Success)
				return serviceResponse;

			record.Name = input.Name;
			record.Description = input.Description;

			var oldCategoryId = -1;

			if (record.CategoryId != newCategoryRecord.Id) {
				var categoryBoards = Records.Where(r => r.CategoryId == record.CategoryId).ToList();

				if (categoryBoards.Count() <= 1)
					oldCategoryId = record.CategoryId;

				record.CategoryId = newCategoryRecord.Id;
			}

			var boardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == record.Id).ToList();

			foreach (var boardRole in boardRoles)
				DbContext.BoardRoles.Remove(boardRole);

			if (input.Roles != null) {
				var roleIds = RoleRepository.SiteRoles.Select(r => r.Id).ToList();

				foreach (var inputRole in input.Roles) {
					if (roleIds.Contains(inputRole)) {
						DbContext.BoardRoles.Add(new DataModels.BoardRole {
							BoardId = record.Id,
							RoleId = inputRole
						});
					}
					else
						serviceResponse.Error($"Role does not exist with id '{inputRole}'");
				}
			}

			if (!serviceResponse.Success)
				return serviceResponse;

			DbContext.Update(record);
			DbContext.SaveChanges();

			if (oldCategoryId >= 0) {
				var oldCategoryRecord = DbContext.Categories.Find(oldCategoryId);
				DbContext.Categories.Remove(oldCategoryRecord);
				DbContext.SaveChanges();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Boards.Manage), nameof(Controllers.Boards), new { id = record.Id });

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse Merge(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromBoard = Records.FirstOrDefault(b => b.Id == input.FromId);
			var toBoard = Records.FirstOrDefault(b => b.Id == input.ToId);

			if (fromBoard is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toBoard is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var messageBoards = DbContext.MessageBoards.Where(m => m.BoardId == fromBoard.Id).ToList();

			// Reassign messages to new board
			foreach (var messageBoard in messageBoards) {
				messageBoard.BoardId = toBoard.Id;
				DbContext.Update(messageBoard);
			}

			DbContext.SaveChanges();

			var categoryId = fromBoard.CategoryId;

			// Delete the board
			DbContext.Boards.Remove(fromBoard);

			DbContext.SaveChanges();

			// Remove the category if empty
			if (!DbContext.Boards.Any(b => b.CategoryId == categoryId)) {
				var categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == categoryId);

				DbContext.Categories.Remove(categoryRecord);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MoveUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetBoard = Records.FirstOrDefault(b => b.Id == id);

			if (targetBoard is null) {
				serviceResponse.Error(string.Empty, "No board found with that ID.");
				return serviceResponse;
			}

			var categoryBoards = Records.Where(b => b.CategoryId == targetBoard.CategoryId).OrderBy(b => b.DisplayOrder).ToList();

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
			else
				targetBoard.DisplayOrder = 2;

			return serviceResponse;
		}
	}
}