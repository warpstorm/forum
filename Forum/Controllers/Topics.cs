using Forum.Controllers.Annotations;
using Forum.Models.Errors;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
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

		[ActionLog("is viewing the topic index.")]
		[HttpGet]
		public async Task<IActionResult> Index(int id = 0, int pageId = 1, int unread = 0) {
			var topicIds = await TopicRepository.GetIndexIds(id, pageId, unread);
			var morePages = true;

			if (topicIds.Count < UserContext.ApplicationUser.TopicsPerPage) {
				morePages = false;
			}

			var topicPreviews = await TopicRepository.GetPreviews(topicIds);

			var boardRecords = await BoardRepository.Records();
			var boardRecord = id == 0 ? null : boardRecords.FirstOrDefault(item => item.Id == id);

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
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> Merge(int id, int pageId = 1) {
			var sourceTopic = DbContext.Topics.FirstOrDefault(item => item.Id == id);

			if (sourceTopic is null || sourceTopic.Deleted) {
				throw new HttpNotFoundError();
			}

			var topicIds = await TopicRepository.GetIndexIds(0, pageId, 0);
			var morePages = true;

			if (topicIds.Count < UserContext.ApplicationUser.TopicsPerPage) {
				morePages = false;
			}

			var topicPreviews = await TopicRepository.GetPreviews(topicIds);

			foreach (var topicPreview in topicPreviews.ToList()) {
				if (topicPreview.Id == id) {
					// Exclude the source topic
					topicPreviews.Remove(topicPreview);
				}
				else {
					// Mark the source topic for all target topics.
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

		[ActionLog("is viewing their bookmarks.")]
		[HttpGet]
		public async Task<IActionResult> Bookmarks() {
			var bookmarkRecords = await BookmarkRepository.Records();
			var topicIds = bookmarkRecords.Select(r => r.TopicId).ToList();
			var topicPreviews = await TopicRepository.GetPreviews(topicIds);

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

		[ActionLog("is viewing a topic.")]
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
		/// Retrieves all of the latest messages in a topic. Useful for API calls.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> DisplayPartial(int id, long latest) {
			var latestTime = new DateTime(latest);

			var topic = DbContext.Topics.Find(id);

			if (topic is null || topic.Deleted) {
				throw new HttpNotFoundError();
			}

			await BoardRepository.GetTopicBoards(id);

			var messageIds = MessageRepository.GetMessageIds(id, latestTime);
			var messages = await MessageRepository.GetMessages(messageIds);

			var latestMessageTime = messages.Max(r => r.RecordTime);
			await TopicRepository.MarkRead(id, latestMessageTime, messageIds);

			var viewModel = new PageModels.TopicDisplayPartialPage {
				Latest = DateTime.Now.Ticks,
				Messages = messages
			};

			return await ForumViewResult.ViewResult(this, "DisplayPartial", viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Latest(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await TopicRepository.GetFirstUnreadMessage(id);
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
				var serviceResponse = await TopicRepository.MarkAllRead();
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> MarkUnread(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await TopicRepository.MarkUnread(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid) {
				await TopicRepository.ToggleBoard(input);
			}

			return new NoContentResult();
		}

		public string GetRedirectPath(int messageId, int topicId, List<int> messageIds) {
			var routeValues = new {
				id = topicId,
				pageId = MessageRepository.GetPageNumber(messageId, messageIds),
				target = messageId
			};

			return UrlHelper.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;
		}

		public async Task<PageModels.TopicDisplayPage> GetDisplayPageModel(int topicId, int pageId = 1, int targetId = -1) {
			var viewModel = new PageModels.TopicDisplayPage();

			var topic = DbContext.Topics.Find(topicId);

			if (topic is null || topic.Deleted) {
				throw new HttpNotFoundError();
			}

			var messageIds = MessageRepository.GetMessageIds(topicId);

			if (targetId >= 0) {
				var targetPage = MessageRepository.GetPageNumber(targetId, messageIds);

				if (targetPage != pageId) {
					viewModel.RedirectPath = GetRedirectPath(targetId, topicId, messageIds);
				}
			}

			if (!string.IsNullOrEmpty(viewModel.RedirectPath)) {
				var bookmarks = await BookmarkRepository.Records();
				var bookmarked = bookmarks.Any(r => r.TopicId == topicId);

				var assignedBoards = await BoardRepository.GetTopicBoards(topicId);

				if (pageId < 1) {
					pageId = 1;
				}

				var take = UserContext.ApplicationUser.MessagesPerPage;
				var skip = take * (pageId - 1);
				var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

				var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

				topic.ViewCount++;
				DbContext.Update(topic);
				DbContext.SaveChanges();

				var messages = await MessageRepository.GetMessages(pageMessageIds);

				viewModel = new PageModels.TopicDisplayPage {
					Id = topic.Id,
					Subject = string.IsNullOrEmpty(topic.FirstMessageShortPreview) ? "No subject" : topic.FirstMessageShortPreview,
					Messages = messages,
					Categories = await BoardRepository.CategoryIndex(),
					AssignedBoards = new List<ViewModels.Boards.Items.IndexBoard>(),
					IsAuthenticated = UserContext.IsAuthenticated,
					IsOwner = topic.FirstMessagePostedById == UserContext.ApplicationUser?.Id,
					IsAdmin = UserContext.IsAdmin,
					IsBookmarked = bookmarked,
					IsPinned = topic.Pinned,
					ShowFavicons = UserContext.ApplicationUser.ShowFavicons,
					TotalPages = totalPages,
					ReplyCount = topic.ReplyCount,
					ViewCount = topic.ViewCount,
					CurrentPage = pageId,
					ReplyForm = new ViewModels.Messages.ReplyForm {
						Id = topic.Id.ToString(),
						ElementId = "topic-reply"
					}
				};

				foreach (var assignedBoard in assignedBoards) {
					var indexBoard = await BoardRepository.GetIndexBoard(assignedBoard);
					viewModel.AssignedBoards.Add(indexBoard);
				}

				var latestMessageTime = messages.Max(r => r.RecordTime);

				await TopicRepository.MarkRead(topicId, latestMessageTime, pageMessageIds);
			}

			return viewModel;
		}
	}
}