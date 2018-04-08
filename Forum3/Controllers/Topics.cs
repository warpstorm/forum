using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Exceptions;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
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

	public class Topics : ForumController {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		BoardRepository BoardRepository { get; }
		CategoryRepository CategoryRepository { get; }
		MessageRepository MessageRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		TopicRepository TopicRepository { get; }

		IUrlHelper UrlHelper { get; }

		public Topics(
			ApplicationDbContext applicationDbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			CategoryRepository categoryRepository,
			MessageRepository messageRepository,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			TopicRepository topicRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = applicationDbContext;
			UserContext = userContext;

			BoardRepository = boardRepository;
			CategoryRepository = categoryRepository;
			MessageRepository = messageRepository;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			TopicRepository = topicRepository;

			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public IActionResult Index(int id = 0, int unread = 0) {
			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == id).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var topicPreviews = TopicRepository.GetPreviews(id, 0, unread);

			var after = 0L;

			if (topicPreviews.Any())
				after = topicPreviews.Min(t => t.LastReplyPostedDT).Ticks;

			var boardRecord = DbContext.Boards.Find(id);

			var viewModel = new PageModels.TopicIndexPage {
				BoardId = id,
				BoardName = boardRecord?.Name ?? "All Topics",
				After = after,
				Topics = topicPreviews,
				UnreadFilter = unread
			};

			return View(viewModel);
		}

		[HttpGet]
		public IActionResult IndexMore(int id = 0, long after = 0, int unread = 0) {
			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == id).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var topicPreviews = TopicRepository.GetPreviews(id, after, unread);

			if (topicPreviews.Any())
				after = topicPreviews.Min(t => t.LastReplyPostedDT).Ticks;
			else
				after = long.MaxValue;

			var viewModel = new PageModels.TopicIndexMorePage {
				More = after != long.MaxValue,
				After = after,
				Topics = topicPreviews
			};

			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Display(int id, int pageId = 1, int target = 0, bool rebuild = false) {
			ViewData["Smileys"] = SmileyRepository.GetSelectorList();

			var viewModel = GetDisplayPageModel(id, pageId, target, rebuild);

			if (string.IsNullOrEmpty(viewModel.RedirectPath))
				return View(viewModel);
			else
				return Redirect(viewModel.RedirectPath);
		}

		[HttpGet]
		public IActionResult Latest(int id) {
			var serviceResponse = TopicRepository.GetLatest(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult Pin(int id) {
			var serviceResponse = TopicRepository.Pin(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult ToggleBoard(InputModels.ToggleBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = TopicRepository.Toggle(input);
				ProcessServiceResponse(serviceResponse);
			}

			return new NoContentResult();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> TopicReply(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.CreateReply(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = GetDisplayPageModel(input.Id);
			viewModel.ReplyForm.Body = input.Body;

			return View(nameof(Display), viewModel);
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

		public string GetMigrationRedirectPath(int messageId) {
			return UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = messageId });
		}

		public PageModels.TopicDisplayPage GetDisplayPageModel(int id, int pageId = 1, int target = 0, bool rebuild = false) {
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

			var unprocessedMessagesQuery = from message in DbContext.Messages
										   where !message.Processed
										   where message.Id == parentId || message.ParentId == parentId || message.LegacyParentId == record.LegacyId
										   where message.LegacyParentId != 0 && message.LegacyId != 0
										   select message.Id;

			if (unprocessedMessagesQuery.Any() || rebuild) {
				viewModel.RedirectPath = GetMigrationRedirectPath(parentId);
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

			var boardRoles = DbContext.BoardRoles.Where(r => assignedBoards.Any(b => b.Id == r.BoardId)).Select(r => r.RoleId).ToList();

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

			viewModel = new PageModels.TopicDisplayPage {
				Id = record.Id,
				TopicHeader = new ItemModels.TopicHeader {
					StartedById = record.PostedById,
					Subject = record.ShortPreview,
					Views = record.ViewCount,
				},
				Messages = messages,
				Categories = CategoryRepository.Index(),
				AssignedBoards = new List<ViewModels.Boards.Items.IndexBoard>(),
				IsAuthenticated = UserContext.IsAuthenticated,
				CanManage = UserContext.IsAdmin || record.PostedById == UserContext.ApplicationUser.Id,
				TotalPages = totalPages,
				CurrentPage = pageId,
				ReplyForm = new ItemModels.ReplyForm {
					Id = record.Id
				}
			};

			foreach (var assignedBoard in assignedBoards) {
				var indexBoard = BoardRepository.Get(assignedBoard);
				viewModel.AssignedBoards.Add(indexBoard);
			}

			var latestMessageTime = messages.Max(r => r.RecordTime);

			TopicRepository.MarkRead(record.Id, latestMessageTime);

			return viewModel;
		}
	}
}