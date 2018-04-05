using Forum3.Contexts;
using Forum3.Controllers;
using Forum3.Enums;
using Forum3.Exceptions;
using Forum3.Extensions;
using Forum3.Processes.Topics;
using Forum3.Services;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.ViewModelProviders.Topics {
	using DataModels = Models.DataModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using PageModels = Models.ViewModels.Topics.Pages;
	using ViewModels = Models.ViewModels;

	public class DisplayPage {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		SettingsRepository Settings { get; }
		LoadTopicPreview TopicPreviewLoader { get; }
		BoardService BoardService { get; }
		IUrlHelper UrlHelper { get; }

		public DisplayPage(
			ApplicationDbContext dbContext,
			UserContext userContext,
			SettingsRepository settingsRepository,
			LoadTopicPreview topicPreviewLoader,
			BoardService boardService,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			Settings = settingsRepository;
			TopicPreviewLoader = topicPreviewLoader;
			BoardService = boardService;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public PageModels.TopicDisplayPage Generate(int messageId, int page = 0, int target = 0, bool rebuild = false) {
			var viewModel = new PageModels.TopicDisplayPage();

			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new HttpNotFoundException($"A record does not exist with ID '{messageId}'");

			var parentId = messageId;

			if (record.ParentId > 0)
				parentId = record.ParentId;

			var messageIdQuery = from message in DbContext.Messages
								 where message.Id == parentId || message.ParentId == parentId
								 select message.Id;

			var messageIds = messageIdQuery.ToList();

			if (parentId != messageId) {
				viewModel.RedirectPath = GetRedirectViewModel(messageId, record.ParentId, messageIds);
				return viewModel;
			}

			var processedQuery = from message in DbContext.Messages
								 where !message.Processed
								 where message.Id == parentId || message.ParentId == parentId || message.LegacyParentId == record.LegacyId
								 where message.LegacyParentId != 0 && message.LegacyId != 0
								 select message.Id;

			if (processedQuery.Any() || rebuild) {
				viewModel.RedirectPath = GetMigrationRedirectViewModel(parentId);
				return viewModel;
			}

			if (target > 0) {
				var targetPage = GetMessagePage(target, messageIds);

				if (targetPage != page) {
					viewModel.RedirectPath = GetRedirectViewModel(target, messageId, messageIds);
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

			if (page < 1)
				page = 1;

			var take = Settings.MessagesPerPage();
			var skip = take * (page - 1);
			var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

			var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

			record.ViewCount++;
			DbContext.Update(record);
			DbContext.SaveChanges();

			var messages = GetTopicMessages(pageMessageIds);

			viewModel = new PageModels.TopicDisplayPage {
				Id = record.Id,
				TopicHeader = new ItemModels.TopicHeader {
					StartedById = record.PostedById,
					Subject = record.ShortPreview,
					Views = record.ViewCount,
				},
				Messages = messages,
				Categories = BoardService.GetCategories(),
				AssignedBoards = new List<ViewModels.Boards.Items.IndexBoard>(),
				IsAuthenticated = UserContext.IsAuthenticated,
				CanManage = UserContext.IsAdmin || record.PostedById == UserContext.ApplicationUser.Id,
				TotalPages = totalPages,
				CurrentPage = page,
				ReplyForm = new ItemModels.ReplyForm {
					Id = record.Id
				}
			};

			foreach (var assignedBoard in assignedBoards) {
				var indexBoard = BoardService.GetIndexBoard(assignedBoard);
				viewModel.AssignedBoards.Add(indexBoard);
			}

			MarkTopicRead(viewModel);

			return viewModel;
		}

		void MarkTopicRead(PageModels.TopicDisplayPage topic) {
			var viewLogs = DbContext.ViewLogs.Where(v =>
				v.UserId == UserContext.ApplicationUser.Id
				&& (v.TargetType == EViewLogTargetType.All || (v.TargetType == EViewLogTargetType.Message && v.TargetId == topic.Id))
			).ToList();

			DateTime latestTime;

			var latestMessageTime = topic.Messages.Max(r => r.RecordTime);

			if (viewLogs.Any()) {
				var latestViewLogTime = viewLogs.Max(r => r.LogTime);
				latestTime = latestViewLogTime > latestMessageTime ? latestViewLogTime : latestMessageTime;
			}
			else
				latestTime = latestMessageTime;

			var historyTimeLimit = Settings.HistoryTimeLimit();

			var existingLogs = viewLogs.Where(r => r.TargetType == EViewLogTargetType.Message);

			foreach (var viewLog in existingLogs)
				DbContext.ViewLogs.Remove(viewLog);

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				LogTime = latestTime,
				TargetId = topic.Id,
				TargetType = EViewLogTargetType.Message,
				UserId = UserContext.ApplicationUser.Id
			});

			//try {
			DbContext.SaveChanges();
			// TODO - uncomment if this problem occurs again.
			// see - https://docs.microsoft.com/en-us/ef/core/saving/concurrency
			// The user probably refreshed several times in a row.
			//catch (DbUpdateConcurrencyException) { }
		}

		string GetRedirectViewModel(int messageId, int parentMessageId, List<int> messageIds) {
			if (parentMessageId == 0)
				parentMessageId = messageId;

			var routeValues = new {
				id = parentMessageId,
				pageId = GetMessagePage(messageId, messageIds),
				target = messageId
			};

			return UrlHelper.Action(nameof(Controllers.Topics.Display), nameof(Controllers.Topics), routeValues) + "#message" + messageId;
		}

		string GetMigrationRedirectViewModel(int messageId) {
			return UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = messageId });
		}

		int GetMessagePage(int messageId, List<int> messageIds) {
			var index = (double) messageIds.FindIndex(id => id == messageId);
			index++;

			var messagesPerPage = Settings.MessagesPerPage();
			return Convert.ToInt32(Math.Ceiling(index / messagesPerPage));
		}

		List<ItemModels.Message> GetTopicMessages(List<int> messageIds) {
			var messageQuery = from message in DbContext.Messages
							   join postedBy in DbContext.Users on message.PostedById equals postedBy.Id
							   join reply in DbContext.Messages on message.ReplyId equals reply.Id into Replies
							   from reply in Replies.DefaultIfEmpty()
							   join replyPostedBy in DbContext.Users on reply.PostedById equals replyPostedBy.Id into RepliesBy
							   from replyPostedBy in RepliesBy.DefaultIfEmpty()
							   where messageIds.Contains(message.Id)
							   orderby message.Id
							   select new ItemModels.Message {
								   Id = message.Id,
								   ParentId = message.ParentId,
								   ReplyId = message.ReplyId,
								   ReplyBody = reply == null ? string.Empty : reply.DisplayBody,
								   ReplyPreview = reply == null ? string.Empty : reply.LongPreview,
								   ReplyPostedBy = replyPostedBy == null ? string.Empty : replyPostedBy.DisplayName,
								   Body = message.DisplayBody,
								   Cards = message.Cards,
								   OriginalBody = message.OriginalBody,
								   PostedByName = postedBy.DisplayName,
								   PostedById = message.PostedById,
								   PostedByAvatarPath = postedBy.AvatarPath,
								   TimePostedDT = message.TimePosted,
								   TimeEditedDT = message.TimeEdited,
								   RecordTime = message.TimeEdited,
								   Processed = message.Processed
							   };

			var messages = messageQuery.ToList();

			foreach (var message in messages) {
				message.TimePosted = message.TimePostedDT.ToPassedTimeString();
				message.TimeEdited = message.TimeEditedDT.ToPassedTimeString();

				message.CanEdit = UserContext.IsAdmin || (UserContext.IsAuthenticated && UserContext.ApplicationUser.Id == message.PostedById);
				message.CanDelete = UserContext.IsAdmin || (UserContext.IsAuthenticated && UserContext.ApplicationUser.Id == message.PostedById);
				message.CanReply = UserContext.IsAuthenticated;
				message.CanThought = UserContext.IsAuthenticated;

				message.ReplyForm = new ItemModels.ReplyForm {
					Id = message.Id,
				};

				var thoughtQuery = from mt in DbContext.MessageThoughts
								   join s in DbContext.Smileys on mt.SmileyId equals s.Id
								   join u in DbContext.Users on mt.UserId equals u.Id
								   where mt.MessageId == message.Id
								   select new ItemModels.MessageThought {
									   Path = s.Path,
									   Thought = s.Thought.Replace("{user}", u.DisplayName)
								   };

				message.Thoughts = thoughtQuery.ToList();
			}

			return messages;
		}
	}
}