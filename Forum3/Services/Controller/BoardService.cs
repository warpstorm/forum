using Forum3.Controllers;
using Forum3.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemViewModels = Models.ViewModels.Boards.Items;
	using PageViewModels = Models.ViewModels.Boards.Pages;
	using ServiceModels = Models.ServiceModels;

	public class BoardService {
		DataModels.ApplicationDbContext DbContext { get; }
		SiteSettingsService SiteSettingsService { get; }
		NotificationService NotificationService { get; }
		ServiceModels.ContextUser ContextUser { get; }
		IUrlHelper UrlHelper { get; }

		public BoardService(
			DataModels.ApplicationDbContext dbContext,
			SiteSettingsService siteSettingsService,
			NotificationService notificationService,
			ContextUserFactory contextUserFactory,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			SiteSettingsService = siteSettingsService;
			NotificationService = notificationService;
			ContextUser = contextUserFactory.GetContextUser();
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<PageViewModels.IndexPage> IndexPage() {
			var birthdays = GetBirthdays();
			var onlineUsers = GetOnlineUsers();
			var notifications = NotificationService.GetNotifications();

			await Task.WhenAll(new Task[] {
				birthdays,
				onlineUsers,
				notifications
			});

			var viewModel = new PageViewModels.IndexPage {
				Birthdays = birthdays.Result.ToArray(),
				Categories = await GetCategories(),
				OnlineUsers = onlineUsers.Result,
				Notifications = notifications.Result
			};

			return viewModel;
		}

		public async Task<PageViewModels.IndexPage> ManagePage() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = await GetCategories()
			};

			return viewModel;
		}

		public async Task<PageViewModels.CreatePage> CreatePage(InputModels.CreateBoardInput input = null) {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = await GetCategoryPickList()
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}

			return viewModel;
		}

		public async Task<PageViewModels.EditPage> EditPage(int boardId, InputModels.EditBoardInput input = null) {
			var record = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == boardId);

			if (record == null)
				throw new Exception($"A record does not exist with ID '{boardId}'");

			var viewModel = new PageViewModels.EditPage() {
				Id = record.Id,
				Categories = await GetCategoryPickList()
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}
			else {
				var category = await DbContext.Categories.FindAsync(record.CategoryId);

				viewModel.Name = record.Name;
				viewModel.Description = record.Description;
				viewModel.Categories.First(item => item.Text == category.Name).Selected = true;
			}

			return viewModel;
		}

		public async Task<ServiceModels.ServiceResponse> Create(InputModels.CreateBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (await DbContext.Boards.AnyAsync(b => b.Name == input.Name))
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			DataModels.Category categoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				categoryRecord = await DbContext.Categories.SingleOrDefaultAsync(c => c.Name == input.NewCategory);

				if (categoryRecord == null) {
					var displayOrder = await DbContext.Categories.DefaultIfEmpty().MaxAsync(c => c.DisplayOrder);

					categoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					await DbContext.Categories.AddAsync(categoryRecord);
				}
			}
			else {
				try {
					var categoryId = Convert.ToInt32(input.Category);
					categoryRecord = await DbContext.Categories.SingleOrDefaultAsync(c => c.Id == categoryId);

					if (categoryRecord == null)
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

			var existingRecord = await DbContext.Boards.SingleOrDefaultAsync(b => b.Name == input.Name);

			if (existingRecord != null)
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			if (!serviceResponse.Success)
				return serviceResponse;

			await DbContext.SaveChangesAsync();

			var record = new DataModels.Board {
				Name = input.Name,
				Description = input.Description,
				CategoryId = categoryRecord.Id
			};

			await DbContext.Boards.AddAsync(record);
			await DbContext.SaveChangesAsync();

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Boards.Manage), nameof(Boards), new { id = record.Id });

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Edit(InputModels.EditBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == input.Id);

			if (record == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.Id}'");

			DataModels.Category newCategoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				newCategoryRecord = await DbContext.Categories.SingleOrDefaultAsync(c => c.Name == input.NewCategory);

				if (newCategoryRecord == null) {
					var displayOrder = await DbContext.Categories.MaxAsync(c => c.DisplayOrder);

					newCategoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					await DbContext.Categories.AddAsync(newCategoryRecord);
				}
			}
			else {
				try {
					var newCategoryId = Convert.ToInt32(input.Category);
					newCategoryRecord = await DbContext.Categories.SingleOrDefaultAsync(c => c.Id == newCategoryId);

					if (newCategoryRecord == null)
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
				var categoryBoards = await DbContext.Boards.Where(r => r.CategoryId == record.CategoryId).ToListAsync();

				if (categoryBoards.Count() <= 1)
					oldCategoryId = record.CategoryId;

				record.CategoryId = newCategoryRecord.Id;
			}

			DbContext.Update(record);
			await DbContext.SaveChangesAsync();

			if (oldCategoryId >= 0) {
				var oldCategoryRecord = DbContext.Categories.Find(oldCategoryId);
				DbContext.Categories.Remove(oldCategoryRecord);
				await DbContext.SaveChangesAsync();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Boards.Manage), nameof(Boards), new { id = record.Id });

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MoveCategoryUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetCategory = DbContext.Categories.FirstOrDefault(b => b.Id == id);

			if (targetCategory == null) {
				serviceResponse.Error(string.Empty, "No category found with that ID.");
				return serviceResponse;
			}

			if (targetCategory.DisplayOrder > 1) {
				var displacedCategory = DbContext.Categories.First(b => b.DisplayOrder == targetCategory.DisplayOrder - 1);

				displacedCategory.DisplayOrder++;
				DbContext.Update(displacedCategory);

				targetCategory.DisplayOrder--;
				DbContext.Update(targetCategory);

				await DbContext.SaveChangesAsync();
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MoveBoardUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetBoard = await DbContext.Boards.FirstOrDefaultAsync(b => b.Id == id);

			if (targetBoard == null) {
				serviceResponse.Error(string.Empty, "No board found with that ID.");
				return serviceResponse;
			}

			var categoryBoards = await DbContext.Boards.Where(b => b.CategoryId == targetBoard.CategoryId).OrderBy(b => b.DisplayOrder).ToListAsync();

			var currentIndex = 1;

			foreach (var board in categoryBoards) {
				board.DisplayOrder = currentIndex++;
				DbContext.Update(board);
			}

			await DbContext.SaveChangesAsync();

			targetBoard = categoryBoards.First(b => b.Id == id);

			if (targetBoard.DisplayOrder > 1) {
				var displacedBoard = categoryBoards.FirstOrDefault(b => b.DisplayOrder == targetBoard.DisplayOrder - 1);

				if (displacedBoard != null) {
					displacedBoard.DisplayOrder++;
					DbContext.Update(displacedBoard);
				}

				targetBoard.DisplayOrder--;
				DbContext.Update(targetBoard);

				await DbContext.SaveChangesAsync();
			}
			else
				targetBoard.DisplayOrder = 2;

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MergeCategory(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromCategory = await DbContext.Categories.SingleOrDefaultAsync(b => b.Id == input.FromId);
			var toCategory = await DbContext.Categories.SingleOrDefaultAsync(b => b.Id == input.ToId);

			if (fromCategory == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toCategory == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var displacedBoards = await DbContext.Boards.Where(b => b.CategoryId == fromCategory.Id).ToListAsync();

			foreach (var displacedBoard in displacedBoards) {
				displacedBoard.CategoryId = toCategory.Id;
				DbContext.Update(displacedBoard);
			}

			await DbContext.SaveChangesAsync();

			DbContext.Categories.Remove(fromCategory);

			await DbContext.SaveChangesAsync();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> MergeBoard(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromBoard = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == input.FromId);
			var toBoard = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == input.ToId);

			if (fromBoard == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toBoard == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var messageBoards = await DbContext.MessageBoards.Where(m => m.BoardId == fromBoard.Id).ToListAsync();

			// Reassign messages to new board
			foreach (var messageBoard in messageBoards) {
				messageBoard.BoardId = toBoard.Id;
				DbContext.Update(messageBoard);
			}

			await DbContext.SaveChangesAsync();

			var categoryId = fromBoard.CategoryId;

			// Delete the board
			DbContext.Boards.Remove(fromBoard);

			await DbContext.SaveChangesAsync();

			// Remove the category if empty
			if (!await DbContext.Boards.AnyAsync(b => b.CategoryId == categoryId)) {
				var categoryRecord = await DbContext.Categories.SingleOrDefaultAsync(c => c.Id == categoryId);

				DbContext.Categories.Remove(categoryRecord);

				await DbContext.SaveChangesAsync();
			}

			return serviceResponse;
		}

		public async Task<List<ItemViewModels.IndexCategory>> GetCategories() {
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
					var indexBoard = await GetIndexBoard(boardRecord);

					// TODO check board roles here

					indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any())
					indexCategories.Add(indexCategory);
			}

			return indexCategories;
		}

		public async Task<ItemViewModels.IndexBoard> GetIndexBoard(DataModels.Board boardRecord) {
			var indexBoard = new ItemViewModels.IndexBoard {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				DisplayOrder = boardRecord.DisplayOrder,
				Unread = false
			};

			if (boardRecord.LastMessageId != null) {
				var lastMessageQuery = from lastReply in DbContext.Messages
									   where lastReply.Id == boardRecord.LastMessageId
									   join lastReplyBy in DbContext.Users on lastReply.LastReplyById equals lastReplyBy.Id
									   select new Models.ViewModels.Topics.Items.MessagePreview {
										   Id = lastReply.Id,
										   ShortPreview = lastReply.ShortPreview,
										   LastReplyByName = lastReplyBy.DisplayName,
										   LastReplyId = lastReply.LastReplyId,
										   LastReplyPosted = lastReply.LastReplyPosted.ToPassedTimeString()
									   };

				indexBoard.LastMessage = await lastMessageQuery.SingleOrDefaultAsync();
			}

			return indexBoard;
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
		
		async Task<List<ItemViewModels.OnlineUser>> GetOnlineUsers() {
			var onlineTimeLimitSetting = await SiteSettingsService.GetInt(Constants.SiteSettings.OnlineTimeLimit);

			if (onlineTimeLimitSetting == 0)
				onlineTimeLimitSetting = Constants.Defaults.OnlineTimeLimit;

			if (onlineTimeLimitSetting > 0)
				onlineTimeLimitSetting *= -1;

			var onlineTimeLimit = DateTime.Now.AddMinutes(onlineTimeLimitSetting);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsers = await (from user in DbContext.Users
									 where user.LastOnline >= onlineTodayTimeLimit
									 orderby user.LastOnline descending
									 select new ItemViewModels.OnlineUser {
										 Id = user.Id,
										 Name = user.DisplayName,
										 Online = user.LastOnline >= onlineTimeLimit,
										 LastOnline = user.LastOnline
									 }).ToListAsync();

			foreach (var onlineUser in onlineUsers)
				onlineUser.LastOnlineString = onlineUser.LastOnline.ToPassedTimeString();

			return onlineUsers;
		}

		async Task<List<string>> GetBirthdays() {
			var todayBirthdayNames = new List<string>();

			var birthdays = await DbContext.Users.Select(u => new Birthday {
				Date = u.Birthday,
				DisplayName = u.DisplayName
			}).ToListAsync();

			if (birthdays.Any()) {
				var todayBirthdays = birthdays.Where(u => new DateTime(DateTime.Now.Year, u.Date.Month, u.Date.Day).Date == DateTime.Now.Date);

				foreach (var item in todayBirthdays) {
					var now = DateTime.Today;
					var age = now.Year - item.Date.Year;

					if (item.Date > now.AddYears(-age))
						age--;

					todayBirthdayNames.Add($"{item.DisplayName} ({age})");
				}
			}

			return todayBirthdayNames;
		}

		class Birthday {
			public string DisplayName { get; set; }
			public DateTime Date { get; set; }
		}
	}
}