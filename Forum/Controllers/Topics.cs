using Forum.Controllers.Annotations;
using Forum.Core.Models.Errors;
using Forum.Data.Contexts;
using Forum.Extensions;
using Forum.Services;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;
	using HubModels = Models.HubModels;
	using InputModels = Models.InputModels;
	using Options = Core.Options;
	using ViewModels = Models.ViewModels;

	public class Topics : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext CurrentUser { get; }
		BoardRepository BoardRepository { get; }
		BookmarkRepository BookmarkRepository { get; }
		MessageRepository MessageRepository { get; }
		TopicRepository TopicRepository { get; }
		IHubContext<ForumHub> ForumHub { get; }

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			BookmarkRepository bookmarkRepository,
			MessageRepository messageRepository,
			TopicRepository topicRepository,
			IHubContext<ForumHub> forumHub
		) {
			DbContext = applicationDbContext;
			CurrentUser = userContext;

			BoardRepository = boardRepository;
			BookmarkRepository = bookmarkRepository;
			MessageRepository = messageRepository;
			TopicRepository = topicRepository;

			ForumHub = forumHub;
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

			return View(viewModel);
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

			return View(viewModel);
		}

		[ActionLog("is starting a new topic.")]
		[HttpGet]
		public async Task<IActionResult> Create(int id = -1) {
			var boards = await BoardRepository.Records();
			var board = boards.FirstOrDefault(item => item.Id == id);

			if (board is null) {
				throw new HttpNotFoundError();
			}

			// Creating a topic via bookmarklet
			if (Request.Query.TryGetValue("source", out var source)) {
				var input = new ControllerModels.Topics.CreateTopicInput {
					Body = source,
					SelectedBoards = new List<int> { id }
				};

				return await Create(input);
			}

			var viewModel = new ViewModels.Topics.CreateTopicForm {
				SelectedBoards = new List<int> { id }
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(ControllerModels.Topics.CreateTopicInput input) {
			if (Request.Method != "GET") {
				foreach (var board in await BoardRepository.Records()) {
					if (Request.Form.TryGetValue("Selected_" + board.Id, out var boardSelected)) {
						if (boardSelected == "True") {
							input.SelectedBoards.Add(board.Id);
						}
					}
				}
			}

			if (input.Action == Options.ECreateTopicSaveAction.AddEvent) {
				var editEventViewModel = new ViewModels.Topics.EditEventForm {
					FormAction = nameof(CreateEvent),
					FormController = nameof(Topics),
					Body = input.Body,
					SelectedBoards = JsonConvert.SerializeObject(input.SelectedBoards)
				};

				ModelState.ClearValidationState(input.Body);

				return View("EditEvent", editEventViewModel);
			}

			if (ModelState.IsValid) {
				var result = await TopicRepository.CreateTopic(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					var redirectPath = Url.DisplayMessage(result.TopicId, result.MessageId);
					return Redirect(redirectPath);
				}
			}

			var viewModel = new ViewModels.Topics.CreateTopicForm {
				Body = input.Body,
				SelectedBoards = input.SelectedBoards,
				AllDay = input.AllDay,
				Start = input.Start,
				End = input.End
			};

			return View(viewModel);
		}

		[ActionLog("is adding an event to a topic.")]
		[HttpGet]
		public IActionResult CreateEvent(int id = -1) {
			var topicRecord = DbContext.Topics.FirstOrDefault(item => item.Id == id);

			if (topicRecord is null) {
				throw new HttpNotFoundError();
			}

			var isOwner = topicRecord.FirstMessagePostedById == CurrentUser.Id;
			var isAdmin = CurrentUser.IsAdmin;

			if (!isOwner && !isAdmin) {
				throw new HttpForbiddenError();
			}

			var viewModel = new ViewModels.Topics.EditEventForm {
				TopicId = id
			};

			return View("EditEvent", viewModel);
		}

		[ActionLog("is adding an event to a topic.")]
		[HttpPost]
		public async Task<IActionResult> CreateEvent(ControllerModels.Topics.EditEventInput input) {
			if (ModelState.IsValid) {
				if (input.TopicId >= 0) {
					var topicRecord = DbContext.Topics.FirstOrDefault(item => item.Id == input.TopicId);

					if (topicRecord is null) {
						throw new HttpNotFoundError();
					}

					if (input.Start is null) {
						return RedirectToAction(nameof(Topics.Display), new { id = topicRecord.Id });
					}

					var isOwner = topicRecord.FirstMessagePostedById == CurrentUser.Id;
					var isAdmin = CurrentUser.IsAdmin;

					if (!isOwner && !isAdmin) {
						throw new HttpForbiddenError();
					}

					var result = await TopicRepository.AddEvent(input);
					ModelState.AddModelErrors(result.Errors);

					if (ModelState.IsValid) {
						var redirectPath = Url.DisplayMessage(result.TopicId, result.MessageId);
						return Redirect(redirectPath);
					}
				}
				else {
					var createTopicViewModel = new ViewModels.Topics.CreateTopicForm {
						Start = input.Start,
						End = input.End,
						AllDay = input.AllDay,
						SelectedBoards = JsonConvert.DeserializeObject<List<int>>(input.SelectedBoards)
					};

					return View(nameof(Create), createTopicViewModel);
				}
			}

			var editEventViewModel = new ViewModels.Topics.EditEventForm {
				FormAction = nameof(Topics.CreateEvent),
				FormController = nameof(Topics),
				Start = input.Start,
				End = input.End,
				AllDay = input.AllDay,
				TopicId = input.TopicId,
				Body = input.Body,
				SelectedBoards = JsonConvert.SerializeObject(input.SelectedBoards)
			};

			return View("EditEvent", editEventViewModel);
		}

		[ActionLog("is editing an event.")]
		[HttpGet]
		public IActionResult EditEvent(int id) {
			var topicRecord = DbContext.Topics.Find(id);
			var eventRecord = DbContext.Events.FirstOrDefault(item => item.TopicId == id);

			if (topicRecord is null) {
				throw new HttpNotFoundError();
			}

			if (eventRecord is null) {
				return RedirectToAction(nameof(CreateEvent), new { id });
			}

			var isOwner = topicRecord.FirstMessagePostedById == CurrentUser.Id;
			var isAdmin = CurrentUser.IsAdmin;

			if (!isOwner && !isAdmin) {
				throw new HttpForbiddenError();
			}

			var editEventViewModel = new ViewModels.Topics.EditEventForm {
				FormAction = nameof(Topics.EditEvent),
				FormController = nameof(Topics),
				Start = eventRecord.Start,
				End = eventRecord.End,
				AllDay = eventRecord.AllDay,
				TopicId = id
			};

			return View(editEventViewModel);
		}

		[HttpPost]
		public async Task<IActionResult> EditEvent(ControllerModels.Topics.EditEventInput input) {
			if (ModelState.IsValid) {
				var topicRecord = DbContext.Topics.Find(input.TopicId);
				var eventRecord = DbContext.Events.FirstOrDefault(item => item.TopicId == input.TopicId);

				if (topicRecord is null || eventRecord is null) {
					throw new HttpNotFoundError();
				}

				var isOwner = topicRecord.FirstMessagePostedById == CurrentUser.Id;
				var isAdmin = CurrentUser.IsAdmin;

				if (!isOwner && !isAdmin) {
					throw new HttpForbiddenError();
				}

				if (input.Start is null || input.End is null) {
					DbContext.Remove(eventRecord);
				}
				else {
					eventRecord.Start = (DateTime)input.Start;
					eventRecord.End = (DateTime)input.End;
					eventRecord.AllDay = input.AllDay;
				}

				await DbContext.SaveChangesAsync();

				if (ModelState.IsValid) {
					return RedirectToAction(nameof(Display), new { id = input.TopicId });
				}
			}

			var editEventViewModel = new ViewModels.Topics.EditEventForm {
				FormAction = nameof(Topics.EditEvent),
				FormController = nameof(Topics),
				Start = input.Start,
				End = input.End,
				AllDay = input.AllDay,
			};

			return View(editEventViewModel);
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

			return View(viewModel);
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> FinishMerge(int sourceId, int targetId) {
			var serviceResponse = await TopicRepository.Merge(sourceId, targetId);
			return await this.RedirectFromService(serviceResponse);
		}

		[ActionLog("is viewing a topic.")]
		[HttpGet]
		public async Task<IActionResult> Display(int id, int page = 1, int target = -1) {
			var topic = await DbContext.Topics.FindAsync(id);

			if (topic is null || topic.Deleted) {
				throw new HttpNotFoundError();
			}

			if (page < 1) {
				var redirectUrl = Url.Action(nameof(Display), new { id, page = 1, target });
				return Redirect(redirectUrl);
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
				ShowFavicons = CurrentUser.ApplicationUser.ShowFavicons ?? true,
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
			await loadEventDetails();

			return View(viewModel);

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

			async Task loadEventDetails() {
				var eventRecord = await DbContext.Events.FirstOrDefaultAsync(item => item.TopicId == topic.Id);

				if (!(eventRecord is null)) {
					viewModel.Start = eventRecord.Start;
					viewModel.End = eventRecord.End;
					viewModel.AllDay = eventRecord.AllDay;
				}
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

			return View("DisplayPartial", viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Latest(int id) {
			var redirectPath = this.GetReferrer();

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
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}

		[HttpGet]
		public async Task<IActionResult> Bookmark(int id) {
			await TopicRepository.Bookmark(id);
			return this.RedirectToReferrer();
		}

		[HttpGet]
		public async Task<IActionResult> MarkAllRead() {
			if (ModelState.IsValid) {
				var serviceResponse = await TopicRepository.MarkAllRead();
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}

		[HttpGet]
		public async Task<IActionResult> MarkUnread(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await TopicRepository.MarkUnread(id);
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}

		[HttpGet]
		public async Task<IActionResult> ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid) {
				await TopicRepository.ToggleBoard(input);
			}

			return new NoContentResult();
		}

		[HttpGet]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		public async Task<IActionResult> Delete(int id) {
			var redirectPath = this.GetReferrer();

			if (ModelState.IsValid) {
				var topic = await DbContext.Topics.SingleAsync(m => m.Id == id);

				await TopicRepository.RemoveTopic(topic);
				await DbContext.SaveChangesAsync();

				await ForumHub.Clients.All.SendAsync("deleted-topic", new HubModels.Message {
					TopicId = topic.Id,
					MessageId = 0
				});

				redirectPath = Url.Action(nameof(Topics.Index), nameof(Topics));
			}

			return Redirect(redirectPath);
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