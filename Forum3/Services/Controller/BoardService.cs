using Forum3.Contexts;
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
		ApplicationDbContext DbContext { get; }
		SettingsRepository Settings { get; }
		NotificationService NotificationService { get; }
		IUrlHelper UrlHelper { get; }

		public BoardService(
			ApplicationDbContext dbContext,
			SettingsRepository SettingsRepository,
			NotificationService notificationService,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			Settings = SettingsRepository;
			NotificationService = notificationService;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public PageViewModels.IndexPage IndexPage() {
			var birthdays = GetBirthdays();
			var onlineUsers = GetOnlineUsers();
			var notifications = NotificationService.GetNotifications();

			var viewModel = new PageViewModels.IndexPage {
				Birthdays = birthdays.ToArray(),
				Categories = GetCategories(),
				OnlineUsers = onlineUsers,
				Notifications = notifications
			};

			return viewModel;
		}

		public PageViewModels.IndexPage ManagePage() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = GetCategories()
			};

			return viewModel;
		}

		public PageViewModels.CreatePage CreatePage(InputModels.CreateBoardInput input = null) {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = GetCategoryPickList()
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}

			return viewModel;
		}

		public PageViewModels.EditPage EditPage(int boardId, InputModels.EditBoardInput input = null) {
			var record = DbContext.Boards.FirstOrDefault(b => b.Id == boardId);

			if (record is null)
				throw new Exception($"A record does not exist with ID '{boardId}'");

			var viewModel = new PageViewModels.EditPage() {
				Id = record.Id,
				Categories = GetCategoryPickList()
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}
			else {
				var category = DbContext.Categories.Find(record.CategoryId);

				viewModel.Name = record.Name;
				viewModel.Description = record.Description;
				viewModel.Categories.First(item => item.Text == category.Name).Selected = true;
			}

			return viewModel;
		}

		public ServiceModels.ServiceResponse Create(InputModels.CreateBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (DbContext.Boards.Any(b => b.Name == input.Name))
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

			var existingRecord = DbContext.Boards.FirstOrDefault(b => b.Name == input.Name);

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

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Boards.Manage), nameof(Boards), new { id = record.Id });

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse Edit(InputModels.EditBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Boards.FirstOrDefault(b => b.Id == input.Id);

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
				var categoryBoards = DbContext.Boards.Where(r => r.CategoryId == record.CategoryId).ToList();

				if (categoryBoards.Count() <= 1)
					oldCategoryId = record.CategoryId;

				record.CategoryId = newCategoryRecord.Id;
			}

			DbContext.Update(record);
			DbContext.SaveChanges();

			if (oldCategoryId >= 0) {
				var oldCategoryRecord = DbContext.Categories.Find(oldCategoryId);
				DbContext.Categories.Remove(oldCategoryRecord);
				DbContext.SaveChanges();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Boards.Manage), nameof(Boards), new { id = record.Id });

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MoveCategoryUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetCategory = DbContext.Categories.FirstOrDefault(b => b.Id == id);

			if (targetCategory is null) {
				serviceResponse.Error(string.Empty, "No category found with that ID.");
				return serviceResponse;
			}

			if (targetCategory.DisplayOrder > 1) {
				var displacedCategory = DbContext.Categories.First(b => b.DisplayOrder == targetCategory.DisplayOrder - 1);

				displacedCategory.DisplayOrder++;
				DbContext.Update(displacedCategory);

				targetCategory.DisplayOrder--;
				DbContext.Update(targetCategory);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MoveBoardUp(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetBoard = DbContext.Boards.FirstOrDefault(b => b.Id == id);

			if (targetBoard is null) {
				serviceResponse.Error(string.Empty, "No board found with that ID.");
				return serviceResponse;
			}

			var categoryBoards = DbContext.Boards.Where(b => b.CategoryId == targetBoard.CategoryId).OrderBy(b => b.DisplayOrder).ToList();

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

		public ServiceModels.ServiceResponse MergeCategory(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromCategory = DbContext.Categories.FirstOrDefault(b => b.Id == input.FromId);
			var toCategory = DbContext.Categories.FirstOrDefault(b => b.Id == input.ToId);

			if (fromCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var displacedBoards = DbContext.Boards.Where(b => b.CategoryId == fromCategory.Id).ToList();

			foreach (var displacedBoard in displacedBoards) {
				displacedBoard.CategoryId = toCategory.Id;
				DbContext.Update(displacedBoard);
			}

			DbContext.SaveChanges();

			DbContext.Categories.Remove(fromCategory);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MergeBoard(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromBoard = DbContext.Boards.FirstOrDefault(b => b.Id == input.FromId);
			var toBoard = DbContext.Boards.FirstOrDefault(b => b.Id == input.ToId);

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
			if (! DbContext.Boards.Any(b => b.CategoryId == categoryId)) {
				var categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == categoryId);

				DbContext.Categories.Remove(categoryRecord);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}

		public List<ItemViewModels.IndexCategory> GetCategories() {
			var categoryRecordsTask = DbContext.Categories.OrderBy(r => r.DisplayOrder).ToListAsync();
			var boardRecordsTask = DbContext.Boards.OrderBy(r => r.DisplayOrder).ToListAsync();

			Task.WaitAll(categoryRecordsTask, boardRecordsTask);

			var categoryRecords = categoryRecordsTask.Result;
			var boardRecords = boardRecordsTask.Result;

			var indexCategories = new List<ItemViewModels.IndexCategory>();

			foreach (var categoryRecord in categoryRecords) {
				var indexCategory = new ItemViewModels.IndexCategory {
					Id = categoryRecord.Id,
					Name = categoryRecord.Name,
					DisplayOrder = categoryRecord.DisplayOrder
				};

				foreach (var boardRecord in boardRecords.Where(r => r.CategoryId == categoryRecord.Id)) {
					var indexBoard = GetIndexBoard(boardRecord);

					// TODO check board roles here

					indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any())
					indexCategories.Add(indexCategory);
			}

			return indexCategories;
		}

		public ItemViewModels.IndexBoard GetIndexBoard(DataModels.Board boardRecord) {
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
										   LastReplyPosted = lastReply.LastReplyPosted.ToPassedTimeString(),
										   LastReplyPreview = lastReply.ShortPreview
									   };

				indexBoard.LastMessage = lastMessageQuery.FirstOrDefault();
			}

			return indexBoard;
		}

		List<SelectListItem> GetCategoryPickList(List<SelectListItem> pickList = null) {
			if (pickList is null)
				pickList = new List<SelectListItem>();

			var categoryRecords = DbContext.Categories.OrderBy(r => r.DisplayOrder).ToList();

			foreach (var categoryRecord in categoryRecords) {
				pickList.Add(new SelectListItem() {
					Text = categoryRecord.Name,
					Value = categoryRecord.Id.ToString()
				});
			}

			return pickList;
		}
		
		List<ItemViewModels.OnlineUser> GetOnlineUsers() {
			var onlineTimeLimitSetting = Settings.OnlineTimeLimit();
			onlineTimeLimitSetting *= -1;

			var onlineTimeLimit = DateTime.Now.AddMinutes(onlineTimeLimitSetting);
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsersQuery = from user in DbContext.Users
								   where user.LastOnline >= onlineTodayTimeLimit
								   orderby user.LastOnline descending
								   select new ItemViewModels.OnlineUser {
									   Id = user.Id,
									   Name = user.DisplayName,
									   Online = user.LastOnline >= onlineTimeLimit,
									   LastOnline = user.LastOnline
								   };

			var onlineUsers = onlineUsersQuery.ToList();

			foreach (var onlineUser in onlineUsers)
				onlineUser.LastOnlineString = onlineUser.LastOnline.ToPassedTimeString();

			return onlineUsers;
		}

		List<string> GetBirthdays() {
			var todayBirthdayNames = new List<string>();

			var birthdays = DbContext.Users.Select(u => new Birthday {
				Date = u.Birthday,
				DisplayName = u.DisplayName
			}).ToList();

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