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

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			BookmarkRepository bookmarkRepository,
			MessageRepository messageRepository,
			RoleRepository roleRepository,
			SmileyRepository smileyRepository,
			TopicRepository topicRepository,
			IForumViewResult forumViewResult
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
		}

		[ActionLog("is viewing the topic index.")]
		[HttpGet]
		public async Task<IActionResult> Index(int id = 0, int page = 1, int unread = 0) {
			var topicIds = await TopicRepository.GetIndexIds(id, page, unread);
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

			var board = (await BoardRepository.Records()).First(item => item.Id == id);

			if (Request.Query.TryGetValue("source", out var source)) {
				return await Create(new InputModels.MessageInput { BoardId = id, Body = source });
			}

			var viewModel = new PageModels.CreateTopicForm {
				Id = "0",
				BoardId = id.ToString()
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
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

				var serviceResponse = await TopicRepository.CreateTopic(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

				var viewModel = new PageModels.CreateTopicForm {
					Id = "0",
					BoardId = input.BoardId.ToString(),
					Body = input.Body
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
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
		public async Task<IActionResult> Display(int id, int page = 1, int target = -1) {
			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var viewModel = await GetDisplayPageModel(id, page, target);

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
			var redirectPath = ForumViewResult.GetReferrer(this);

			if (ModelState.IsValid) {
				var target = await TopicRepository.GetTopicTargetMessageId(id);

				var routeValues = new {
					id = id,
					page = 1,
					target = target
				};

				redirectPath = Url.Action(nameof(Topics.Display), nameof(Topics), routeValues);
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

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> RebuildTopics(InputModels.MultiStepInput input) {
			var take = 250;
			var topicCount = await DbContext.Topics.CountAsync();
			var totalPages = Convert.ToInt32(Math.Floor(1d * topicCount / take));

			var viewModel = new ViewModels.MultiStep {
				ActionName = "Rebuilding Topics",
				ActionNote = "Recounting replies, calculating participants, determining first and last messages.",
				Action = Url.Action(nameof(RebuildTopicsContinue)),
				TotalPages = totalPages,
				TotalRecords = topicCount,
				Take = take,
			};

			return await ForumViewResult.ViewResult(this, "MultiStep", viewModel);
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> RebuildTopicsContinue(InputModels.MultiStepInput input) {
			var topicsQuery = from topic in DbContext.Topics
						 where !topic.Deleted
						 select topic;

			topicsQuery = topicsQuery.Skip(input.Page * input.Take).Take(input.Take);

			foreach (var topic in topicsQuery) {
				await TopicRepository.RebuildTopic(topic);
			}

			return Ok();
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Admin() => await ForumViewResult.ViewResult(this);

		public string GetRedirectPath(int messageId, int topicId, List<int> messageIds) {
			var routeValues = new {
				id = topicId,
				page = MessageRepository.GetPageNumber(messageId, messageIds),
				target = messageId
			};

			return Url.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;
		}

		public async Task<PageModels.TopicDisplayPage> GetDisplayPageModel(int topicId, int page = 1, int targetId = -1) {
			var viewModel = new PageModels.TopicDisplayPage();

			var topic = DbContext.Topics.Find(topicId);

			if (topic is null || topic.Deleted) {
				throw new HttpNotFoundError();
			}

			var messageIds = MessageRepository.GetMessageIds(topicId);

			if (targetId >= 0) {
				var targetPage = MessageRepository.GetPageNumber(targetId, messageIds);

				if (targetPage != page) {
					viewModel.RedirectPath = GetRedirectPath(targetId, topicId, messageIds);
				}
			}

			if (!string.IsNullOrEmpty(viewModel.RedirectPath)) {
				var bookmarks = await BookmarkRepository.Records();
				var bookmarked = bookmarks.Any(r => r.TopicId == topicId);

				var assignedBoards = await BoardRepository.GetTopicBoards(topicId);

				if (page < 1) {
					page = 1;
				}

				var take = UserContext.ApplicationUser.MessagesPerPage;
				var skip = take * (page - 1);
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
					CurrentPage = page,
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