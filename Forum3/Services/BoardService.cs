using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Forum3.Data;
using Forum3.Helpers;
using DataModels = Forum3.Models.DataModels;
using InputModels = Forum3.Models.InputModels;
using PageViewModels = Forum3.Models.ViewModels.Boards.Pages;
using ItemViewModels = Forum3.Models.ViewModels.Boards.Items;
using Microsoft.EntityFrameworkCore;
using Forum3.Models.ServiceModels;

namespace Forum3.Services {
	public class BoardService {
		ApplicationDbContext DbContext { get; }
		UserService UserService { get; }

		public BoardService(
			ApplicationDbContext dbContext,
			UserService userService
		) {
			DbContext = dbContext;
			UserService = userService;
		}

		public async Task<PageViewModels.IndexPage> IndexPage() {
			var birthdays = UserService.GetBirthdays();
			var onlineUsers = UserService.GetOnlineUsers();

			await Task.WhenAll(new Task[] {
				birthdays,
				onlineUsers
			});

			var viewModel = new PageViewModels.IndexPage {
				Birthdays = birthdays.Result.ToArray(),
				Categories = await GetCategories(),
				OnlineUsers = onlineUsers.Result
			};

			return viewModel;
		}

		public async Task<PageViewModels.IndexPage> ManagePage() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = await GetCategories()
			};

			return viewModel;
		}

		public async Task<PageViewModels.CreatePage> CreatePage(InputModels.BoardInput input = null) {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = await GetCategoryPickList()
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.VettedOnly = input.VettedOnly;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}

			return viewModel;
		}

		public async Task Create(InputModels.BoardInput input, ModelStateDictionary modelState) {
			if (DbContext.Boards.Any(b => b.Name == input.Name))
				modelState.AddModelError(nameof(input.Name), "A board with that name already exists");

			DataModels.Category categoryRecord = null;

			var categoryId = 0;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (categoryRecord == null) {
					var displayOrder = await DbContext.Categories.MaxAsync(c => c.DisplayOrder);

					categoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					await DbContext.Categories.AddAsync(categoryRecord);
				}
			}
			else {
				try {
					categoryId = Convert.ToInt32(input.Category);

					categoryRecord = DbContext.Categories.Find(categoryId);

					if (categoryRecord == null)
						modelState.AddModelError(nameof(input.Category), "No category was found with this ID.");
				}
				catch (FormatException) {
					modelState.AddModelError(nameof(input.Category), "Invalid category ID");
				}
			}

			input.Name = input.Name.Trim();

			if (string.IsNullOrEmpty(input.Name))
				modelState.AddModelError(nameof(input.Name), "Name is a required field.");

			var existingRecord = DbContext.Boards.FirstOrDefault(b => b.Name == input.Name);

			if (existingRecord != null)
				modelState.AddModelError(nameof(input.Name), "A board with that name already exists");

			if (!modelState.IsValid)
				return;

			await DbContext.SaveChangesAsync();

			var boardRecord = new DataModels.Board {
				Name = input.Name,
				VettedOnly = input.VettedOnly,
				CategoryId = categoryRecord.Id
			};

			if (modelState.IsValid) {
				await DbContext.Boards.AddAsync(boardRecord);
				await DbContext.SaveChangesAsync();
			}
		}

		public async Task<ServiceResponse> MoveCategoryUp(int id) {
			var serviceResponse = new ServiceResponse();
			
			var targetCategory = DbContext.Categories.FirstOrDefault(b => b.Id == id);

			if (targetCategory == null) {
				serviceResponse.ModelErrors.Add(string.Empty, "No category found with that ID.");
				return serviceResponse;
			}

			if (targetCategory.DisplayOrder > 1) {
				var displacedCategory = DbContext.Categories.First(b => b.DisplayOrder == targetCategory.DisplayOrder - 1);

				displacedCategory.DisplayOrder++;
				DbContext.Entry(displacedCategory).State = EntityState.Modified;

				targetCategory.DisplayOrder--;
				DbContext.Entry(targetCategory).State = EntityState.Modified;

				await DbContext.SaveChangesAsync();
			}

			return serviceResponse;
		}

		public async Task<ServiceResponse> MoveBoardUp(int id) {
			var serviceResponse = new ServiceResponse();

			var targetBoard = await DbContext.Boards.FirstOrDefaultAsync(b => b.Id == id);

			if (targetBoard == null) {
				serviceResponse.ModelErrors.Add(string.Empty, "No board found with that ID.");
				return serviceResponse;
			}

			var categoryBoards = await DbContext.Boards.Where(b => b.CategoryId == targetBoard.CategoryId).OrderBy(b => b.DisplayOrder).ToListAsync();

			var currentIndex = 1;

			foreach (var board in categoryBoards) {
				board.DisplayOrder = currentIndex++;
				DbContext.Entry(board).State = EntityState.Modified;
			}

			await DbContext.SaveChangesAsync();

			targetBoard = categoryBoards.First(b => b.Id == id);

			if (targetBoard.DisplayOrder > 1) {
				var displacedBoard = categoryBoards.FirstOrDefault(b => b.DisplayOrder == targetBoard.DisplayOrder - 1);

				if (displacedBoard != null) {
					displacedBoard.DisplayOrder++;
					DbContext.Entry(displacedBoard).State = EntityState.Modified;
				}

				targetBoard.DisplayOrder--;
				DbContext.Entry(targetBoard).State = EntityState.Modified;

				await DbContext.SaveChangesAsync();
			}
			else
				targetBoard.DisplayOrder = 2;

			return serviceResponse;
		}

		async Task<List<SelectListItem>> GetCategoryPickList(List<SelectListItem> pickList = null) {
			if (pickList == null)
				pickList = new List<SelectListItem>();

			var categoryRecords = await DbContext.Categories.OrderBy(r => r.DisplayOrder).ToListAsync();

			foreach (var categoryRecord in categoryRecords) {
				pickList.Add(new SelectListItem() {
					Text = categoryRecord.Name,
					Value = categoryRecord.Id.ToString()
				});
			}

			return pickList;
		}

		async Task<List<ItemViewModels.IndexCategory>> GetCategories(int? targetBoard = null) {
			var categoryRecords = await DbContext.Categories.OrderBy(r => r.DisplayOrder).ToListAsync();
			var boardRecords = await DbContext.Boards.OrderBy(r => r.DisplayOrder).ToListAsync();

			var indexCategories = new List<ItemViewModels.IndexCategory>();

			foreach (var categoryRecord in categoryRecords) {
				var indexCategory = new ItemViewModels.IndexCategory {
					Id = categoryRecord.Id,
					Name = categoryRecord.Name,
					DisplayOrder = categoryRecord.DisplayOrder
				};

				foreach (var boardRecord in boardRecords.Where(r => r.CategoryId == categoryRecord.Id)) {
					var indexBoard = GetIndexBoard(targetBoard, boardRecord);

					if (!indexBoard.VettedOnly || UserService.ContextUser.IsVetted)
						indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any())
					indexCategories.Add(indexCategory);
			}

			return indexCategories;
		}

		ItemViewModels.IndexBoard GetIndexBoard(int? targetBoard, DataModels.Board boardRecord) {
			var indexBoard = new ItemViewModels.IndexBoard {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				DisplayOrder = boardRecord.DisplayOrder,
				VettedOnly = boardRecord.VettedOnly,
				Unread = false,
				Selected = targetBoard != null && targetBoard == boardRecord.Id,
			};

			if (boardRecord.LastMessage != null) {
				indexBoard.LastMessage = new Models.ViewModels.Topics.Items.MessagePreview {
					Id = boardRecord.LastMessage.Id,
					ShortPreview = boardRecord.LastMessage.ShortPreview,
					LastReplyByName = boardRecord.LastMessage.LastReplyByName,
					LastReplyId = boardRecord.LastMessage.LastReplyId,
					LastReplyPosted = boardRecord.LastMessage.LastReplyPosted.ToPassedTimeString()
				};
			}

			return indexBoard;
		}
	}
}