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

		BoardRepository BoardRepository { get; }
		BookmarkRepository BookmarkRepository { get; }
		MessageRepository MessageRepository { get; }
		RoleRepository RoleRepository { get; }
		SmileyRepository SmileyRepository { get; }
		TopicRepository TopicRepository { get; }

		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			BookmarkRepository bookmarkRepository,
			MessageRepository messageRepository,
			RoleRepository roleRepository,
			SmileyRepository smileyRepository,
			TopicRepository topicRepository,
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = applicationDbContext;
			UserContext = userContext;

			BoardRepository = boardRepository;
			BookmarkRepository = bookmarkRepository;
			MessageRepository = messageRepository;
			RoleRepository = roleRepository;
			SmileyRepository = smileyRepository;
			TopicRepository = topicRepository;

			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public async Task<IActionResult> Index(int id = 0, int pageId = 1, int unread = 0) {
			var messageIds = await TopicRepository.GetIndexIds(id, pageId, unread);
			var morePages = true;

			if (messageIds.Count < UserContext.ApplicationUser.TopicsPerPage) {
				morePages = false;
			}

			var topicPreviews = await TopicRepository.GetPreviews(messageIds);

			var boardRecord = id == 0 ? null : (await BoardRepository.Records()).FirstOrDefault(record => record.Id == id);

			var viewModel = new PageModels.TopicIndexPage {
				BoardId = id,
				BoardName = boardRecord?.Name ?? "All Topics",
				CurrentPage = pageId,
				Topics = topicPreviews,
				UnreadFilter = unread,
				MorePages = morePages
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> IndexPartial(int id = 0, int pageId = 0, int unread = 0) {
			var messageIds = await TopicRepository.GetIndexIds(id, pageId, unread);
			var morePages = true;

			if (messageIds.Count < UserContext.ApplicationUser.TopicsPerPage) {
				morePages = false;
			}

			var topicPreviews = await TopicRepository.GetPreviews(messageIds);

			var viewModel = new PageModels.TopicIndexPage {
				BoardId = id,
				UnreadFilter = unread,
				CurrentPage = pageId,
				Topics = topicPreviews,
				MorePages = morePages,
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> Merge(int id, int pageId = 1) {
			var record = DbContext.Messages.FirstOrDefault(item => item.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var messageIds = await TopicRepository.GetIndexIds(0, pageId, 0);
			var morePages = true;

			if (messageIds.Count < UserContext.ApplicationUser.TopicsPerPage) {
				morePages = false;
			}

			var topicPreviews = await TopicRepository.GetPreviews(messageIds);

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
				CurrentPage = pageId,
				Topics = topicPreviews,
				MorePages = morePages
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Bookmarks() {
			var messageIds = (await BookmarkRepository.Records()).Select(r => r.MessageId).ToList();
			var topicPreviews = await TopicRepository.GetPreviews(messageIds);

			var viewModel = new PageModels.TopicBookmarksPage {
				Topics = topicPreviews
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> FinishMerge(int sourceId, int targetId) {
			var serviceResponse = await TopicRepository.Merge(sourceId, targetId);
			return await ForumViewResult.RedirectFromService(this, serviceResponse);
		}

		[HttpGet]
		public async Task<IActionResult> Display(int id, int pageId = 1, int target = -1) {
			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var viewModel = await GetDisplayPageModel(id, pageId, target);

			if (string.IsNullOrEmpty(viewModel.RedirectPath)) {
				return await ForumViewResult.ViewResult(this, viewModel);
			}
			else {
				return Redirect(viewModel.RedirectPath);
			}
		}

		/// <summary>
		/// Retrieves a specific message. Useful for API calls.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> DisplayOne(int id) {
			var record = DbContext.Messages.Find(id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var topicId = id;

			if (record.ParentId > 0) {
				topicId = record.ParentId;
			}

			await LoadTopicBoards(topicId);

			var messageIds = new List<int> { id };
			var messages = await MessageRepository.GetMessages(messageIds);

			var viewModel = new PageModels.TopicDisplayPartialPage {
				Latest = DateTime.Now.Ticks,
				Messages = messages
			};

			return await ForumViewResult.ViewResult(this, "DisplayPartial", viewModel);
		}

		/// <summary>
		/// Retrieves all of the latest messages in a topic. Useful for API calls.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> DisplayPartial(int id, long latest) {
			var latestTime = new DateTime(latest);

			var record = DbContext.Messages.Find(id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var topicId = id;

			if (record.ParentId > 0) {
				topicId = record.ParentId;
			}

			await LoadTopicBoards(topicId);

			var messageIds = MessageRepository.GetMessageIds(topicId, latestTime);
			var messages = await MessageRepository.GetMessages(messageIds);

			var latestMessageTime = messages.Max(r => r.RecordTime);
			TopicRepository.MarkRead(topicId, latestMessageTime, messageIds);

			var viewModel = new PageModels.TopicDisplayPartialPage {
				Latest = DateTime.Now.Ticks,
				Messages = messages
			};

			return await ForumViewResult.ViewResult(this, "DisplayPartial", viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Latest(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.GetLatest(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Pin(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await TopicRepository.Pin(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> Bookmark(int id) {
			await TopicRepository.Bookmark(id);
			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> MarkAllRead() {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.MarkAllRead();
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> MarkUnread(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.MarkUnread(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid) {
				await TopicRepository.Toggle(input);
			}

			return new NoContentResult();
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

		public async Task<PageModels.TopicDisplayPage> GetDisplayPageModel(int id, int pageId = 1, int targetId = -1) {
			var viewModel = new PageModels.TopicDisplayPage();

			var record = DbContext.Messages.Find(id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var topicId = id;

			if (record.ParentId > 0) {
				topicId = record.ParentId;
			}

			var messageIds = MessageRepository.GetMessageIds(topicId);

			if (topicId != id) {
				viewModel.RedirectPath = GetRedirectPath(id, record.ParentId, messageIds);
			}
			else if (targetId >= 0) {
				var targetPage = MessageRepository.GetPageNumber(targetId, messageIds);

				if (targetPage != pageId) {
					viewModel.RedirectPath = GetRedirectPath(targetId, id, messageIds);
				}
			}

			if (string.IsNullOrEmpty(viewModel.RedirectPath)) {
				var bookmarked = (await BookmarkRepository.Records()).Any(r => r.MessageId == topicId);

				var assignedBoards = await LoadTopicBoards(topicId);

				if (pageId < 1) {
					pageId = 1;
				}

				var take = UserContext.ApplicationUser.MessagesPerPage;
				var skip = take * (pageId - 1);
				var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

				var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

				record.ViewCount++;
				DbContext.Update(record);
				DbContext.SaveChanges();

				var messages = await MessageRepository.GetMessages(pageMessageIds);

				if (string.IsNullOrEmpty(record.ShortPreview)) {
					record.ShortPreview = "No subject";
				}

				viewModel = new PageModels.TopicDisplayPage {
					Id = record.Id,
					TopicHeader = new ItemModels.TopicHeader {
						StartedById = record.PostedById,
						Subject = record.ShortPreview,
						Views = record.ViewCount,
					},
					Messages = messages,
					Categories = await BoardRepository.CategoryIndex(),
					AssignedBoards = new List<ViewModels.Boards.Items.IndexBoard>(),
					IsAuthenticated = UserContext.IsAuthenticated,
					IsOwner = record.PostedById == UserContext.ApplicationUser?.Id,
					IsAdmin = UserContext.IsAdmin,
					IsBookmarked = bookmarked,
					IsPinned = record.Pinned,
					ShowFavicons = UserContext.ApplicationUser.ShowFavicons,
					TotalPages = totalPages,
					ReplyCount = record.ReplyCount,
					ViewCount = record.ViewCount,
					CurrentPage = pageId,
					ReplyForm = new ViewModels.Messages.ReplyForm {
						Id = record.Id.ToString(),
						ElementId = $"topic-reply"
					}
				};

				foreach (var assignedBoard in assignedBoards) {
					var indexBoard = await BoardRepository.GetIndexBoard(assignedBoard);
					viewModel.AssignedBoards.Add(indexBoard);
				}

				var latestMessageTime = messages.Max(r => r.RecordTime);

				TopicRepository.MarkRead(topicId, latestMessageTime, pageMessageIds);
			}

			return viewModel;
		}

		public async Task<List<Models.DataModels.Board>> LoadTopicBoards(int topicId) {
			var messageBoardsQuery = from messageBoard in DbContext.MessageBoards
									 where messageBoard.MessageId == topicId
									 select messageBoard.BoardId;

			var boardIds = messageBoardsQuery.ToList();
			var assignedBoards = (await BoardRepository.Records()).Where(r => boardIds.Contains(r.Id)).ToList();

			if (!await RoleRepository.CanAccessBoards(assignedBoards)) {
				throw new HttpForbiddenError();
			}

			return assignedBoards;
		}
	}
}