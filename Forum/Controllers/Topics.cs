using Forum.Annotations;
using Forum.Contexts;
using Forum.Errors;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Forum.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using PageModels = Models.ViewModels.Topics.Pages;
	using ViewModels = Models.ViewModels;

	public class Topics : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		Sidebar Sidebar { get; }

		BoardRepository BoardRepository { get; }
		MessageRepository MessageRepository { get; }
		RoleRepository RoleRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		TopicRepository TopicRepository { get; }

		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			MessageRepository messageRepository,
			RoleRepository roleRepository,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			TopicRepository topicRepository,
			Sidebar sidebar,
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = applicationDbContext;
			UserContext = userContext;

			BoardRepository = boardRepository;
			MessageRepository = messageRepository;
			RoleRepository = roleRepository;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			TopicRepository = topicRepository;

			Sidebar = sidebar;
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public IActionResult Index(int id = 0, int unread = 0) {
			var boardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == id).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any()) {
				throw new HttpForbiddenError();
			}

			var sidebar = Sidebar.Generate();

			var page = 1;
			var topicPreviews = TopicRepository.GetPreviews(id, page, unread);

			var boardRecord = id == 0 ? null : BoardRepository.FirstOrDefault(record => record.Id == id);

			var viewModel = new PageModels.TopicIndexPage {
				BoardId = id,
				BoardName = boardRecord?.Name ?? "All Topics",
				Page = page,
				Topics = topicPreviews,
				UnreadFilter = unread,
				Sidebar = sidebar
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult IndexMore(int id = 0, int page = 0, int unread = 0) {
			var boardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == id).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any()) {
				throw new HttpForbiddenError();
			}

			var topicPreviews = TopicRepository.GetPreviews(id, page, unread);

			ViewData[Constants.InternalKeys.Layout] = "_LayoutEmpty";

			var viewModel = new PageModels.TopicIndexMorePage {
				More = topicPreviews.Any(),
				Page = page,
				Topics = topicPreviews
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public IActionResult Merge(int id) {
			var record = DbContext.Messages.FirstOrDefault(item => item.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var topicPreviews = TopicRepository.GetPreviews(0, 1, 0);

			foreach (var topicPreview in topicPreviews.ToList()) {
				if (topicPreview.Id == id) {
					topicPreviews.Remove(topicPreview);
				}
				else {
					topicPreview.SourceId = id;
				}
			}

			var viewModel = new PageModels.TopicIndexPage {
				SourceId = id,
				BoardName = "Pick a Destination Topic",
				BoardId = 0,
				Page = 1,
				Topics = topicPreviews,
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public IActionResult MergeMore(int id, int page = 0) {
			var record = DbContext.Messages.FirstOrDefault(item => item.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var topicPreviews = TopicRepository.GetPreviews(0, page, 0);

			foreach (var topicPreview in topicPreviews.ToList()) {
				if (topicPreview.Id == id) {
					topicPreviews.Remove(topicPreview);
				}
				else {
					topicPreview.SourceId = id;
				}
			}

			var viewModel = new PageModels.TopicIndexMorePage {
				More = topicPreviews.Any(),
				Page = page,
				Topics = topicPreviews
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> FinishMerge(int sourceId, int targetId) {
			var serviceResponse = TopicRepository.Merge(sourceId, targetId);
			return await ForumViewResult.RedirectFromService(this, serviceResponse, FailToReferrer);
		}

		[HttpGet]
		public IActionResult Display(int id, int pageId = 1, int target = 0) {
			ViewData["Smileys"] = SmileyRepository.GetSelectorList();

			var viewModel = GetDisplayPageModel(id, pageId, target);

			if (string.IsNullOrEmpty(viewModel.RedirectPath)) {
				return ForumViewResult.ViewResult(this, viewModel);
			}
			else {
				return Redirect(viewModel.RedirectPath);
			}
		}

		[HttpGet]
		public IActionResult DisplayPartial(int id, long latest) {
			var latestTime = new DateTime(latest);

			var record = DbContext.Messages.Find(id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var topicId = id;

			if (record.ParentId > 0) {
				topicId = record.ParentId;
			}

			var assignedBoardsQuery = from messageBoard in DbContext.MessageBoards
									  join board in DbContext.Boards on messageBoard.BoardId equals board.Id
									  where messageBoard.MessageId == topicId
									  select board;

			var assignedBoards = assignedBoardsQuery.ToList();

			if (!RoleRepository.CanAccessBoards(assignedBoards)) {
				throw new HttpForbiddenError();
			}

			var messageIds = MessageRepository.GetMessageIds(topicId, latestTime);

			var messageList = TopicRepository.GetMessages(messageIds);

			if (!messageList.Any()) {
				throw new HttpNotFoundError();
			}

			ViewData[Constants.InternalKeys.Layout] = "_LayoutEmpty";

			var viewModel = new PageModels.TopicDisplayPartialPage {
				Latest = DateTime.Now.Ticks,
				Messages = messageList
			};

			return ForumViewResult.ViewResult(this, "DisplayPartial", viewModel);
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
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailToReferrer);
			}

			return await FailToReferrer();
		}

		[HttpGet]
		public async Task<IActionResult> MarkAllRead() {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.MarkAllRead();
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailToReferrer);
			}

			return await FailToReferrer();
		}

		[HttpGet]
		public async Task<IActionResult> MarkUnread(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.MarkUnread(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailToReferrer);
			}

			return await FailToReferrer();
		}

		[HttpGet]
		public IActionResult ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid) {
				TopicRepository.Toggle(input);
			}

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
			if (parentMessageId == 0) {
				parentMessageId = messageId;
			}

			var routeValues = new {
				id = parentMessageId,
				pageId = MessageRepository.GetPageNumber(messageId, messageIds),
				target = messageId
			};

			return UrlHelper.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;
		}

		public PageModels.TopicDisplayPage GetDisplayPageModel(int id, int pageId = 1, int targetId = -1) {
			var viewModel = new PageModels.TopicDisplayPage();

			var record = DbContext.Messages.Find(id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var parentId = id;

			if (record.ParentId > 0) {
				parentId = record.ParentId;
			}

			var messageIds = MessageRepository.GetMessageIds(parentId);

			if (parentId != id) {
				viewModel.RedirectPath = GetRedirectPath(id, record.ParentId, messageIds);
			}
			else if (targetId >= 0) {
				var targetPage = MessageRepository.GetPageNumber(targetId, messageIds);

				if (targetPage != pageId) {
					viewModel.RedirectPath = GetRedirectPath(targetId, id, messageIds);
				}
			}

			if (string.IsNullOrEmpty(viewModel.RedirectPath)) {
				var assignedBoardsQuery = from messageBoard in DbContext.MessageBoards
										  join board in DbContext.Boards on messageBoard.BoardId equals board.Id
										  where messageBoard.MessageId == record.Id
										  select board;

				var assignedBoards = assignedBoardsQuery.ToList();

				if (!RoleRepository.CanAccessBoards(assignedBoards)) {
					throw new HttpForbiddenError();
				}

				if (pageId < 1) {
					pageId = 1;
				}

				var take = SettingsRepository.MessagesPerPage();
				var skip = take * (pageId - 1);
				var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

				var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

				record.ViewCount++;
				DbContext.Update(record);
				DbContext.SaveChanges();

				var messages = TopicRepository.GetMessages(pageMessageIds);

				if (string.IsNullOrEmpty(record.ShortPreview)) {
					record.ShortPreview = "No subject";
				}

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
					CanManage = UserContext.IsAdmin || record.PostedById == UserContext.ApplicationUser?.Id,
					TotalPages = totalPages,
					ReplyCount = record.ReplyCount,
					ViewCount = record.ViewCount,
					CurrentPage = pageId,
					ShowFavicons = showFavicons,
					ReplyForm = new ItemModels.ReplyForm {
						Id = record.Id.ToString()
					}
				};

				foreach (var assignedBoard in assignedBoards) {
					var indexBoard = BoardRepository.GetIndexBoard(assignedBoard);
					viewModel.AssignedBoards.Add(indexBoard);
				}

				var latestMessageTime = messages.Max(r => r.RecordTime);

				TopicRepository.MarkRead(record.Id, latestMessageTime, pageMessageIds);
			}

			return viewModel;
		}

		async Task<IActionResult> FailToReferrer() => await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
	}
}