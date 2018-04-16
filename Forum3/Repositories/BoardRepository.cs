using Forum3.Contexts;
using Forum3.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
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
		public List<DataModels.Category> Categories { get; }

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		RoleRepository RoleRepository { get; }
		IUrlHelper UrlHelper { get; }

		public BoardRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			RoleRepository roleRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			RoleRepository = roleRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);

			Records = DbContext.Boards.OrderBy(record => record.DisplayOrder).ToList();
			Categories = DbContext.Categories.OrderBy(record => record.DisplayOrder).ToList();
		}

		public List<SelectListItem> CategoryPickList() {
			var pickList = new List<SelectListItem>();

			foreach (var categoryRecord in Categories) {
				pickList.Add(new SelectListItem {
					Text = categoryRecord.Name,
					Value = categoryRecord.Id.ToString()
				});
			}

			return pickList;
		}

		public List<ItemViewModels.IndexCategory> CategoryIndex(bool includeReplies = false) {
			var categories = Categories.OrderBy(r => r.DisplayOrder).ToList();

			var indexCategories = new List<ItemViewModels.IndexCategory>();

			foreach (var categoryRecord in categories) {
				var indexCategory = new ItemViewModels.IndexCategory {
					Id = categoryRecord.Id,
					Name = categoryRecord.Name,
					DisplayOrder = categoryRecord.DisplayOrder
				};

				foreach (var board in Records.Where(r => r.CategoryId == categoryRecord.Id)) {
					var thisBoardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == board.Id);

					var authorized = UserContext.IsAdmin || !thisBoardRoles.Any() || (UserContext.Roles?.Any(userRole => thisBoardRoles.Any(boardRole => boardRole.RoleId == userRole)) ?? false);

					if (!authorized)
						continue;

					var indexBoard = GetIndexBoard(board, includeReplies);

					indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any())
					indexCategories.Add(indexCategory);
			}

			return indexCategories;
		}

		public ItemViewModels.IndexBoard GetIndexBoard(DataModels.Board boardRecord, bool includeReplies = false) {
			var indexBoard = new ItemViewModels.IndexBoard {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				DisplayOrder = boardRecord.DisplayOrder,
				Unread = false
			};

			if (includeReplies) {
				var messages = from messageBoard in DbContext.MessageBoards
							   join message in DbContext.Messages on messageBoard.MessageId equals message.Id
							   where messageBoard.BoardId == boardRecord.Id
							   orderby message.LastReplyPosted descending
							   select new {
								   messageBoard.MessageId,
								   message.LastReplyId,
							   };

				// Only checks the most recent 10 topics. If all 10 are forbidden, then LastMessage stays null.
				foreach (var item in messages.Take(10)) {
					var messageRoles = from messageBoard in DbContext.MessageBoards
									   join boardRole in DbContext.BoardRoles on messageBoard.BoardId equals boardRole.BoardId
									   where messageBoard.MessageId == item.MessageId
									   select boardRole.RoleId;

					if (UserContext.IsAdmin || !messageRoles.Any() || messageRoles.Intersect(UserContext.Roles).Any()) {
						var lastReply = from message in DbContext.Messages
										join lastReplyBy in DbContext.Users on message.PostedById equals lastReplyBy.Id
										where message.Id == item.MessageId
										select new Models.ViewModels.Topics.Items.MessagePreview {
											Id = message.Id,
											ShortPreview = message.ShortPreview,
											LastReplyByName = lastReplyBy.DisplayName,
											LastReplyId = message.LastReplyId,
											LastReplyPosted = message.LastReplyPosted.ToPassedTimeString(),
											LastReplyPreview = message.ShortPreview
										};

						indexBoard.LastMessage = lastReply.FirstOrDefault();
						break;
					}
				}
			}

			return indexBoard;
		}

		public ServiceModels.ServiceResponse AddBoard(InputModels.CreateBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (Records.Any(b => b.Name == input.Name))
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			DataModels.Category categoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				categoryRecord = Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (categoryRecord is null) {
					var displayOrder = Categories.DefaultIfEmpty().Max(c => c.DisplayOrder);

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
					categoryRecord = Categories.FirstOrDefault(c => c.Id == categoryId);

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

		public ServiceModels.ServiceResponse UpdateBoard(InputModels.EditBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = Records.FirstOrDefault(b => b.Id == input.Id);

			if (record is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.Id}'");

			DataModels.Category newCategoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				newCategoryRecord = Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (newCategoryRecord is null) {
					var displayOrder = Categories.DefaultIfEmpty().Max(c => c.DisplayOrder);

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
					newCategoryRecord = Categories.FirstOrDefault(c => c.Id == newCategoryId);

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
				var oldCategoryRecord = Categories.FirstOrDefault(item => item.Id == oldCategoryId);
				DbContext.Categories.Remove(oldCategoryRecord);
				DbContext.SaveChanges();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Boards.Manage), nameof(Controllers.Boards), new { id = record.Id });

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MergeBoard(InputModels.MergeInput input) {
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
				var categoryRecord = Categories.FirstOrDefault(item => item.Id == categoryId);

				DbContext.Categories.Remove(categoryRecord);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MergeCategory(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromCategory = Categories.FirstOrDefault(b => b.Id == input.FromId);
			var toCategory = Categories.FirstOrDefault(b => b.Id == input.ToId);

			if (fromCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var displacedBoards = Records.Where(b => b.CategoryId == fromCategory.Id).ToList();

			foreach (var displacedBoard in displacedBoards) {
				displacedBoard.CategoryId = toCategory.Id;
				DbContext.Update(displacedBoard);
			}

			DbContext.SaveChanges();

			DbContext.Categories.Remove(fromCategory);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MoveBoardUp(int id) {
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

		public ServiceModels.ServiceResponse MoveCategoryUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetCategory = Categories.FirstOrDefault(b => b.Id == id);

			if (targetCategory is null) {
				serviceResponse.Error(string.Empty, "No category found with that ID.");
				return serviceResponse;
			}

			if (targetCategory.DisplayOrder > 1) {
				var displacedCategory = Categories.First(b => b.DisplayOrder == targetCategory.DisplayOrder - 1);

				displacedCategory.DisplayOrder++;
				DbContext.Update(displacedCategory);

				targetCategory.DisplayOrder--;
				DbContext.Update(targetCategory);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}
	}
}