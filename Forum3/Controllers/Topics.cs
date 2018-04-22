using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Exceptions;
using Forum3.Interfaces.Services;
using Forum3.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using PageModels = Models.ViewModels.Topics.Pages;
	using ViewModels = Models.ViewModels;

	public class Topics : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		MessageRepository MessageRepository { get; }
		RoleRepository RoleRepository { get; }
		NotificationRepository NotificationRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		TopicRepository TopicRepository { get; }

		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			BoardRepository boardRepository,
			MessageRepository messageRepository,
			NotificationRepository notificationRepository,
			RoleRepository roleRepository,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			TopicRepository topicRepository,
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = applicationDbContext;
			UserContext = userContext;

			AccountRepository = accountRepository;
			BoardRepository = boardRepository;
			MessageRepository = messageRepository;
			NotificationRepository = notificationRepository;
			RoleRepository = roleRepository;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			TopicRepository = topicRepository;

			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public IActionResult Index(int id = 0, int unread = 0) {
			var birthdays = AccountRepository.GetBirthdaysList();
			var onlineUsers = AccountRepository.GetOnlineList();
			var notifications = NotificationRepository.Index();
			
			var boardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == id).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var page = 1;
			var topicPreviews = TopicRepository.GetPreviews(id, page, unread);

			var boardRecord = BoardRepository.FirstOrDefault(record => record.Id == id);

			var viewModel = new PageModels.TopicIndexPage {
				BoardId = id,
				BoardName = boardRecord?.Name ?? "All Topics",
				Page = page,
				Topics = topicPreviews,
				UnreadFilter = unread,
				Birthdays = birthdays.ToArray(),
				OnlineUsers = onlineUsers,
				Notifications = notifications
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult IndexMore(int id = 0, int page = 0, int unread = 0) {
			var boardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == id).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var topicPreviews = TopicRepository.GetPreviews(id, page, unread);

			var viewModel = new PageModels.TopicIndexMorePage {
				More = topicPreviews.Any(),
				Page = page,
				Topics = topicPreviews
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult Display(int id, int pageId = 1, int target = 0) {
			ViewData["Smileys"] = SmileyRepository.GetSelectorList();

			var viewModel = GetDisplayPageModel(id, pageId, target);

			if (string.IsNullOrEmpty(viewModel.RedirectPath))
				return ForumViewResult.ViewResult(this, viewModel);
			else
				return Redirect(viewModel.RedirectPath);
		}

		[HttpGet]
		public async Task<IActionResult> Latest(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.GetLatest(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Pin(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.Pin(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> MarkUnread(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.MarkUnread(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[HttpGet]
		public IActionResult ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid)
				TopicRepository.Toggle(input);

			return new NoContentResult();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.CreateReply(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = GetDisplayPageModel(input.Id);
				viewModel.ReplyForm.Body = input.Body;

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, nameof(Display), viewModel); });
			}
		}

		public string GetRedirectPath(int messageId, int parentMessageId, List<int> messageIds) {
			if (parentMessageId == 0)
				parentMessageId = messageId;

			var routeValues = new {
				id = parentMessageId,
				pageId = MessageRepository.GetPageNumber(messageId, messageIds),
				target = messageId
			};

			return UrlHelper.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;
		}

		public PageModels.TopicDisplayPage GetDisplayPageModel(int id, int pageId = 1, int target = 0) {
			var viewModel = new PageModels.TopicDisplayPage();

			var record = DbContext.Messages.Find(id);

			if (record is null)
				throw new HttpNotFoundException($"A record does not exist with ID '{id}'");

			var parentId = id;

			if (record.ParentId > 0)
				parentId = record.ParentId;

			var messageIdQuery = from message in DbContext.Messages
								 where message.Id == parentId || message.ParentId == parentId
								 select message.Id;

			var messageIds = messageIdQuery.ToList();

			if (parentId != id) {
				viewModel.RedirectPath = GetRedirectPath(id, record.ParentId, messageIds);
				return viewModel;
			}

			if (target > 0) {
				var targetPage = MessageRepository.GetPageNumber(target, messageIds);

				if (targetPage != pageId) {
					viewModel.RedirectPath = GetRedirectPath(target, id, messageIds);
					return viewModel;
				}
			}

			var assignedBoardsQuery = from messageBoard in DbContext.MessageBoards
									  join board in DbContext.Boards on messageBoard.BoardId equals board.Id
									  where messageBoard.MessageId == record.Id
									  select board;

			var assignedBoards = assignedBoardsQuery.ToList();

			var boardRoles = RoleRepository.BoardRoles.Where(r => assignedBoards.Any(b => b.Id == r.BoardId)).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this topic.");

			if (pageId < 1)
				pageId = 1;

			var take = SettingsRepository.MessagesPerPage();
			var skip = take * (pageId - 1);
			var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

			var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

			record.ViewCount++;
			DbContext.Update(record);
			DbContext.SaveChanges();

			var messages = TopicRepository.GetMessages(pageMessageIds);

			if (string.IsNullOrEmpty(record.ShortPreview))
				record.ShortPreview = "No subject";

			var showFavicons = SettingsRepository.ShowFavicons();

			viewModel = new PageModels.TopicDisplayPage {
				Id = record.Id,
				TopicHeader = new ItemModels.TopicHeader {
					StartedById = record.PostedById,
					Subject = record.ShortPreview,
					Views = record.ViewCount,
				},
				Messages = messages,
				Categories = BoardRepository.CategoryIndex(),
				AssignedBoards = new List<ViewModels.Boards.Items.IndexBoard>(),
				IsAuthenticated = UserContext.IsAuthenticated,
				CanManage = UserContext.IsAdmin || record.PostedById == UserContext.ApplicationUser.Id,
				TotalPages = totalPages,
				ReplyCount = record.ReplyCount,
				ViewCount = record.ViewCount,
				CurrentPage = pageId,
				ShowFavicons = showFavicons,
				ReplyForm = new ItemModels.ReplyForm {
					Id = record.Id
				}
			};

			foreach (var assignedBoard in assignedBoards) {
				var indexBoard = BoardRepository.GetIndexBoard(assignedBoard);
				viewModel.AssignedBoards.Add(indexBoard);
			}

			var latestMessageTime = messages.Max(r => r.RecordTime);

			TopicRepository.MarkRead(record.Id, latestMessageTime, pageMessageIds);

			return viewModel;
		}
	}
}