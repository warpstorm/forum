using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Forum3.Data;
using Forum3.Enums;
using Forum3.Helpers;
using PageViewModels = Forum3.ViewModels.Boards.Pages;
using ItemViewModels = Forum3.ViewModels.Boards.Items;

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

		public PageViewModels.IndexPage GetIndexPage() {
			var boards = LoadBoardSummaries();
			var onlineUsers = UserService.GetOnlineUsers();
			
			var viewModel = new PageViewModels.IndexPage {
				Birthdays = UserService.GetBirthdays().ToArray(),
				Boards = boards,
				OnlineUsers = onlineUsers
			};

			return viewModel;
		}

		public PageViewModels.CreatePage GetCreatePage(InputModels.BoardInput input = null) {
			var viewModel = new PageViewModels.CreatePage();
			viewModel.Parents = GetBoardPickList();

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.VettedOnly = input.VettedOnly;

				if (!string.IsNullOrEmpty(input.Parent))
					viewModel.Parents.First(item => item.Value == input.Parent).Selected = true;
			}

			return viewModel;
		}

		public async Task Create(InputModels.BoardInput input, ModelStateDictionary modelState) {
			if (DbContext.Boards.Any(b => b.Name == input.Name))
				modelState.AddModelError(nameof(input.Name), "A board with that name already exists");

			DataModels.Board parentRecord = null;

			if (!string.IsNullOrEmpty(input.Parent)) {
				try {
					var parentId = Convert.ToInt32(input.Parent);

					parentRecord = DbContext.Boards.Find(parentId);

					if (parentRecord == null)
						modelState.AddModelError(nameof(input.Parent), "No parent was found with this name.");
				}
				catch (FormatException) {
					modelState.AddModelError(nameof(input.Parent), "Invalid parent ID");
				}
			}

			if (!modelState.IsValid)
				return;

			var boardRecord = new DataModels.Board {
				Name = input.Name,
				VettedOnly = input.VettedOnly
			};

			if (parentRecord != null)
				boardRecord.ParentId = parentRecord.Id;

			if (modelState.IsValid) {
				await DbContext.Boards.AddAsync(boardRecord);
				await DbContext.SaveChangesAsync();
			}
		}

		public List<SelectListItem> GetBoardPickList(int depth = 0, int? parentId = null, List<SelectListItem> pickList = null, List<DataModels.Board> boardRecords = null) {
			if (pickList == null)
				pickList = new List<SelectListItem>();

			if (boardRecords == null)
				boardRecords = DbContext.Boards.ToList();

			foreach (var board in boardRecords.Where(r => r.ParentId == parentId).OrderBy(r => r.DisplayOrder).ToList()) {
				if (board.VettedOnly && UserService.ContextUser.IsVetted)
					continue;

				var padding = "";

				for (int i = 0; i < depth; i++)
					padding += " ";

				var selectListItem = new SelectListItem();
				selectListItem.Text = padding + board.Name;
				selectListItem.Value = board.Id.ToString();

				pickList.Add(selectListItem);
			}

			foreach (var board in boardRecords.Where(r => r.ParentId == parentId).OrderBy(r => r.DisplayOrder).ToList())
				GetBoardPickList(depth + 1, board.Id, pickList, boardRecords);

			return pickList;
		}

		public List<ItemViewModels.IndexBoardSummary> LoadBoardSummaries(int? targetBoard = null) {
			List<DataModels.Board> boardRecords = DbContext.Boards.ToList();
			List<DataModels.Message> lastMessages = null;
			List<ItemViewModels.IndexUser> lastMessagesBy = null;
			List<DataModels.ViewLog> boardViewLogs = null;

			if (DbContext != null) {
				var lastMessageIds = boardRecords.Select(r => r.LastMessageId).ToList();
				lastMessages = DbContext.Messages.Where(r => lastMessageIds.Contains(r.Id)).ToList();

				var lastMessagesByIds = lastMessages.Select(m => m.LastReplyById);

				lastMessagesBy = DbContext.Users.Where(r => lastMessagesByIds.Contains(r.Id)).Select(r => new ItemViewModels.IndexUser {
					Id = r.Id,
					Name = r.DisplayName
				}).ToList();

				if (UserService.ContextUser.IsAuthenticated)
					boardViewLogs = DbContext.ViewLogs.Where(r => r.UserId == UserService.ContextUser.Id && r.TargetType == EViewLogTargetType.Board).ToList();
			}

			var boards = new List<ItemViewModels.IndexBoardSummary>();

			foreach (var board in boardRecords.Where(r => r.ParentId == null).OrderBy(r => r.DisplayOrder).ToList()) {
				if (!board.VettedOnly || UserService.ContextUser.IsVetted) {
					boards.Add(LoadBoard(targetBoard, board, boardRecords, lastMessages, lastMessagesBy, boardViewLogs));
				}
			}

			return boards;
		}

		ItemViewModels.IndexBoardSummary LoadBoard(int? targetBoard, DataModels.Board board, List<DataModels.Board> boards, List<DataModels.Message> lastMessages = null, List<ItemViewModels.IndexUser> lastMessagesBy = null, List<DataModels.ViewLog> boardViewLogs = null) {
			var indexBoard = new ItemViewModels.IndexBoardSummary {
				Id = board.Id,
				Name = board.Name,
				DisplayOrder = board.DisplayOrder,
				Parent = board.ParentId,
				VettedOnly = board.VettedOnly,
				Unread = false,
				Selected = targetBoard != null && targetBoard == board.Id,
				Children = new List<ItemViewModels.IndexBoardSummary>()
			};

			if (lastMessages != null && lastMessagesBy != null) {
				var lastMessage = lastMessages.FirstOrDefault(r => r.Id == board.LastMessageId);

				if (lastMessage != null) {
					indexBoard.LastMessage = new ViewModels.Topics.Items.MessagePreview {
						Id = lastMessage.Id,
						ShortPreview = lastMessage.ShortPreview,
						LastReplyByName = lastMessagesBy.First(r => r.Id == lastMessage.LastReplyById).Name,
						LastReplyId = lastMessage.LastReplyId,
						LastReplyPosted = lastMessage.LastReplyPosted.ToPassedTimeString()
					};

					if (boardViewLogs != null) {
						var viewLog = boardViewLogs.FirstOrDefault(r => r.TargetId == board.Id);

						if (viewLog == null || lastMessage.LastReplyPosted > viewLog.LogTime)
							indexBoard.Unread = true;
					}
				}
			}

			var boardChildren = boards.Where(r => r.ParentId == board.Id).OrderBy(r => r.DisplayOrder).ToList();

			foreach (var childRecord in boardChildren)
				if (!childRecord.VettedOnly || UserService.ContextUser.IsVetted)
					indexBoard.Children.Add(LoadBoard(targetBoard, childRecord, boards, lastMessages, lastMessagesBy, boardViewLogs));

			return indexBoard;
		}
	}
}