using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Models.Errors;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Helpers;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Topics : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext CurrentUser { get; }
		BoardRepository BoardRepository { get; }
		BookmarkRepository BookmarkRepository { get; }
		MessageRepository MessageRepository { get; }
		SmileyRepository SmileyRepository { get; }
		TopicRepository TopicRepository { get; }
		ForumViewResult ForumViewResult { get; }

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			BookmarkRepository bookmarkRepository,
			MessageRepository messageRepository,
			SmileyRepository smileyRepository,
			TopicRepository topicRepository,
			ForumViewResult forumViewResult
		) {
			DbContext = applicationDbContext;
			CurrentUser = userContext;

			BoardRepository = boardRepository;
			BookmarkRepository = bookmarkRepository;
			MessageRepository = messageRepository;
			SmileyRepository = smileyRepository;
			TopicRepository = topicRepository;

			ForumViewResult = forumViewResult;
		}

		[ActionLog("is viewing the topic index.")]
		[HttpGet]
		public async Task<IActionResult> Index(int id = 0, int page = 1, int unread = 0) {
			var topicIds = await TopicRepository.GetIndexIds(id, page, unread);
			var morePages = true;

			if (topicIds.Count < CurrentUser.ApplicationUser.TopicsPerPage) {
				morePages = false;
			}

			var topicPreviews = await TopicRepository.GetPreviews(topicIds);

			var boardRecords = await BoardRepository.Records();
			var boardRecord = id == 0 ? null : boardRecords.FirstOrDefault(item => item.Id == id);

			var viewModel = new ViewModels.Topics.TopicIndexPage {
				BoardId = id,
				BoardName = boardRecord?.Name ?? "All Topics",
				CurrentPage = page,
				Topics = topicPreviews,
				UnreadFilter = unread,
				MorePages = morePages
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> Merge(int id, int page = 1) {
			var sourceTopic = DbContext.Topics.FirstOrDefault(item => item.Id == id);

			if (sourceTopic is null || sourceTopic.Deleted) {
				throw new HttpNotFoundError();
			}

			var topicIds = await TopicRepository.GetIndexIds(0, page, 0);
			var morePages = true;

			if (topicIds.Count < CurrentUser.ApplicationUser.TopicsPerPage) {
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

			var viewModel = new ViewModels.Topics.TopicIndexPage {
				SourceId = id,
				BoardName = "Pick a Destination Topic",
				BoardId = 0,
				CurrentPage = page,
				Topics = topicPreviews,
				MorePages = morePages
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[ActionLog("is starting a new topic.")]
		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var boards = await BoardRepository.Records();
			var board = boards.First(item => item.Id == id);

			// Creating a topic via bookmarklet
			if (Request.Query.TryGetValue("source", out var source)) {
				return await Create(new ControllerModels.Topics.CreateTopicInput { BoardId = id, Body = source });
			}

			var viewModel = new ViewModels.Topics.CreateTopicForm {
				BoardId = id.ToString()
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(ControllerModels.Topics.CreateTopicInput input) {
			if (ModelState.IsValid) {
				// Creating a topic via bookmarklet
				if (Request.Method == "GET" && input.BoardId != null) {
					input.SelectedBoards.Add((int)input.BoardId);
				}
				else {
					foreach (var board in await BoardRepository.Records()) {
						if (Request.Form.TryGetValue("Selected_" + board.Id, out var boardSelected)) {
							if (boardSelected == "True") {
								input.SelectedBoards.Add(board.Id);
							}
						}
					}
				}

				var result = await TopicRepository.CreateTopic(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					var redirectPath = Url.DisplayMessage(result.TopicId, result.MessageId);
					return Redirect(redirectPath);
				}
			}

			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var viewModel = new ViewModels.Topics.CreateTopicForm {
				BoardId = input.BoardId.ToString(),
				Body = input.Body
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[ActionLog("is viewing their bookmarks.")]
		[HttpGet]
		public async Task<IActionResult> Bookmarks() {
			var bookmarkRecords = await BookmarkRepository.Records();
			var topicIds = bookmarkRecords.Select(r => r.TopicId).ToList();
			var topicPreviews = await TopicRepository.GetPreviews(topicIds);

			var viewModel = new ViewModels.Topics.TopicBookmarksPage {
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
		public async Task<IActionResult> Display(int id, int page = 1, int target = -1) {
			var topic = await DbContext.Topics.FindAsync(id);

			if (topic is null || topic.Deleted) {
				throw new HttpNotFoundError();
			}

			if (page < 1) {
				throw new HttpBadRequestError("Page numbers must be >= 1.");
			}

			var messageIds = MessageRepository.GetMessageIds(topic.Id);

			if (target >= 0) {
				var targetPage = MessageRepository.GetPageNumber(target, messageIds);

				if (targetPage != page) {
					return Redirect(GetRedirectPath(id, targetPage, target));
				}
			}

			var take = CurrentUser.ApplicationUser.MessagesPerPage;
			var skip = take * (page - 1);
			var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));
			var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

			var viewModel = new ViewModels.Topics.TopicDisplayPage {
				Id = topic.Id,
				FirstMessageId = topic.FirstMessageId,
				Subject = string.IsNullOrEmpty(topic.FirstMessageShortPreview) ? "No subject" : topic.FirstMessageShortPreview,
				AssignedBoards = new List<ViewModels.Boards.IndexBoard>(),
				IsAuthenticated = CurrentUser.IsAuthenticated,
				IsOwner = topic.FirstMessagePostedById == CurrentUser.Id,
				IsAdmin = CurrentUser.IsAdmin,
				IsPinned = topic.Pinned,
				ShowFavicons = CurrentUser.ApplicationUser.ShowFavicons,
				TotalPages = totalPages,
				ReplyCount = topic.ReplyCount,
				ViewCount = topic.ViewCount,
				CurrentPage = page,
				ReplyForm = new ViewModels.Messages.ReplyForm {
					Id = "0",
					TopicId = topic.Id.ToString(),
					ElementId = "topic-reply"
				}
			};

			await isBookmarked();
			await loadMessages();
			await loadCategories();
			await loadTopicBoards();
			await loadSmileyViewData();

			return await ForumViewResult.ViewResult(this, viewModel);

			async Task isBookmarked() {
				viewModel.IsBookmarked = await BookmarkRepository.IsBookmarked(topic.Id);
			}

			async Task loadMessages() {
				viewModel.Messages = await MessageRepository.GetMessages(pageMessageIds);

				var latestMessageTime = viewModel.Messages.Max(r => r.RecordTime);
				await TopicRepository.MarkRead(topic.Id, latestMessageTime, pageMessageIds);

				topic.ViewCount++;
				DbContext.Update(topic);
				await DbContext.SaveChangesAsync();
			}

			async Task loadCategories() {
				viewModel.Categories = await BoardRepository.CategoryIndex();
			}

			async Task loadTopicBoards() {
				var topicBoards = await BoardRepository.GetTopicBoards(topic.Id);

				foreach (var topicBoard in topicBoards) {
					var indexBoard = await BoardRepository.GetIndexBoard(topicBoard);
					viewModel.AssignedBoards.Add(indexBoard);
				}
			}

			async Task loadSmileyViewData() {
				ViewData[Constants.InternalKeys.SmileyViewData] = await SmileyRepository.GetSelectorList();
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

			var viewModel = new ViewModels.Topics.TopicDisplayPartialPage {
				Latest = DateTime.Now.Ticks,
				Messages = messages
			};

			return await ForumViewResult.ViewResult(this, "DisplayPartial", viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Latest(int id) {
			var redirectPath = ForumViewResult.GetReferrer(this);

			if (ModelState.IsValid) {
				var target = await TopicRepository.GetTopicTargetMessageId(id);
				redirectPath = GetRedirectPath(id, 1, target);
			}

			return Redirect(redirectPath);
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

		string GetRedirectPath(int id, int page, int target) {
			var routeValues = new {
				id,
				page,
				target
			};

			return Url.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + target;
		}
	}
}