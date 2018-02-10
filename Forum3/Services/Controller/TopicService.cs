using Forum3.Contexts;
using Forum3.Controllers;
using Forum3.Enums;
using Forum3.Exceptions;
using Forum3.Extensions;
using Forum3.Models.InputModels;
using Forum3.Models.ViewModels.Boards.Items;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using PageModels = Models.ViewModels.Topics.Pages;
	using ServiceModels = Models.ServiceModels;

	public class TopicService {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		BoardService BoardService { get; }
		SettingsRepository Settings { get; }
		IUrlHelper UrlHelper { get; }

		public TopicService(
			ApplicationDbContext dbContext,
			UserContext userContext,
			BoardService boardService,
			SettingsRepository settingsRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			BoardService = boardService;
			Settings = settingsRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public PageModels.TopicIndexPage IndexPage(int boardId) {
			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == boardId).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var messageIds = GetIndexIds(boardId, 0);

			var topicPreviews = GetTopicPreviews(messageIds);

			var after = 0L;

			if (topicPreviews.Any())
				after = topicPreviews.Min(t => t.LastReplyPostedDT).Ticks;

			var boardRecord = DbContext.Boards.Find(boardId);

			return new PageModels.TopicIndexPage {
				BoardId = boardId,
				BoardName = boardRecord?.Name ?? "All Topics",
				After = after,
				Topics = topicPreviews
			};
		}

		public PageModels.TopicIndexMorePage IndexMore(int boardId, long after, int unreadFilter = 0) {
			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == boardId).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any())
				throw new HttpForbiddenException("You are not authorized to view this board.");

			var messageIds = GetIndexIds(boardId, after);

			var topicPreviews = GetTopicPreviews(messageIds);

			if (topicPreviews.Any())
				after = topicPreviews.Min(t => t.LastReplyPostedDT).Ticks;
			else
				after = long.MaxValue;

			return new PageModels.TopicIndexMorePage {
				More = after != long.MaxValue,
				After = after,
				Topics = topicPreviews
			};
		}

		public PageModels.TopicIndexPage UnreadPage(int boardId) {
			if (!UserContext.IsAuthenticated)
				throw new ApplicationException("User must be logged in to view this page.");

			var boardRecord = DbContext.Boards.Find(boardId);

			var messageIdQuery = from message in DbContext.Messages
								 orderby message.LastReplyPosted descending
								 join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
								 where boardRecord == null || messageBoard.BoardId == boardRecord.Id
								 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
								 from pin in pins.DefaultIfEmpty()
								 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
								 orderby (pinned ? pin.Id : 0) descending, message.LastReplyPosted descending
								 select message.Id;

			var pageMessageIds = messageIdQuery.ToList();

			var topicPreviews = GetTopicPreviews(pageMessageIds);

			return new PageModels.TopicIndexPage {
				BoardId = boardRecord?.Id ?? 0,
				BoardName = boardRecord?.Name ?? "All Topics",
				Topics = topicPreviews
			};
		}

		public PageModels.TopicDisplayPage DisplayPage(int messageId, int page = 0, int target = 0) {
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

			if (parentId != messageId)
				return GetRedirectViewModel(messageId, record.ParentId, messageIds);

			var processedQuery = from message in DbContext.Messages
								 where !message.Processed
								 where message.Id == parentId || message.ParentId == parentId || message.LegacyParentId == record.LegacyId
								 where message.LegacyParentId != 0 && message.LegacyId != 0
								 select message.Id;

			if (processedQuery.Any())
				return GetMigrationRedirectViewModel(messageId);

			if (target > 0) {
				var targetPage = GetMessagePage(target, messageIds);

				if (targetPage != page)
					return GetRedirectViewModel(target, messageId, messageIds);
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

			var topic = new PageModels.TopicDisplayPage {
				Id = record.Id,
				TopicHeader = new ItemModels.TopicHeader {
					StartedById = record.PostedById,
					Subject = record.ShortPreview,
					Views = record.ViewCount,
				},
				Messages = messages,
				Categories = BoardService.GetCategories(),
				AssignedBoards = new List<IndexBoard>(),
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
				topic.AssignedBoards.Add(indexBoard);
			}

			MarkTopicRead(topic);

			return topic;
		}

		public ServiceModels.ServiceResponse Latest(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new HttpNotFoundException($@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0)
				record = DbContext.Messages.Find(record.ParentId);

			if (!UserContext.IsAuthenticated) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.LastReplyId });
				return serviceResponse;
			}

			var historyTimeLimit = Settings.HistoryTimeLimit();
			var viewLogs = DbContext.ViewLogs.Where(r => r.UserId == UserContext.ApplicationUser.Id && r.LogTime >= historyTimeLimit).ToList();
			var latestViewTime = historyTimeLimit;

			foreach (var viewLog in viewLogs) {
				switch (viewLog.TargetType) {
					case EViewLogTargetType.All:
						if (viewLog.LogTime >= latestViewTime)
							latestViewTime = viewLog.LogTime;
						break;

					case EViewLogTargetType.Message:
						if (viewLog.TargetId == record.Id && viewLog.LogTime >= latestViewTime)
							latestViewTime = viewLog.LogTime;
						break;
				}
			}

			var messageIdQuery = from message in DbContext.Messages
								 where message.Id == record.Id || message.ParentId == record.Id
								 where message.TimePosted >= latestViewTime
								 select message.Id;

			var latestMessageId = messageIdQuery.FirstOrDefault();

			if (latestMessageId == 0)
				latestMessageId = record.LastReplyId;

			if (latestMessageId == 0)
				latestMessageId = record.Id;

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = latestMessageId });

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse Pin(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new HttpNotFoundException($@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0)
				messageId = record.ParentId;

			var existingRecord = DbContext.Pins.FirstOrDefault(p => p.MessageId == messageId && p.UserId == UserContext.ApplicationUser.Id);

			if (existingRecord is null) {
				var pinRecord = new DataModels.Pin {
					MessageId = messageId,
					Time = DateTime.Now,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.Pins.Add(pinRecord);
			}
			else
				DbContext.Pins.Remove(existingRecord);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse ToggleBoard(ToggleBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = DbContext.Messages.Find(input.MessageId);

			if (messageRecord is null)
				throw new HttpNotFoundException($@"No message was found with the id '{input.MessageId}'");

			var messageId = input.MessageId;

			if (messageRecord.ParentId > 0)
				messageId = messageRecord.ParentId;

			if (!DbContext.Boards.Any(r => r.Id == input.BoardId))
				serviceResponse.Error(string.Empty, $@"No board was found with the id '{input.BoardId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var boardId = input.BoardId;

			var existingRecord = DbContext.MessageBoards.FirstOrDefault(p => p.MessageId == messageId && p.BoardId == boardId);

			if (existingRecord is null) {
				var messageBoardRecord = new DataModels.MessageBoard {
					MessageId = messageId,
					BoardId = boardId,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.MessageBoards.Add(messageBoardRecord);
			}
			else
				DbContext.MessageBoards.Remove(existingRecord);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		List<int> GetIndexIds(int boardId, long after) {
			var take = Settings.TopicsPerPage();

			var forbiddenBoardIdsQuery = from role in DbContext.Roles
										 join board in DbContext.BoardRoles on role.Id equals board.RoleId
										 where !UserContext.Roles.Contains(role.Id)
										 select board.BoardId;

			var forbiddenBoardIds = forbiddenBoardIdsQuery.ToList();

			IQueryable<DataModels.Message> messageQuery = null;

			if (boardId > 0) {
				messageQuery = from message in DbContext.Messages
							   join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
							   where messageBoard.BoardId == boardId
							   select message;
			}
			else
				messageQuery = DbContext.Messages;

			var afterTarget = new DateTime(after);

			IQueryable<int> messageIdQuery = null;

			if (afterTarget == default(DateTime)) {
				messageIdQuery = from message in messageQuery
								 where message.ParentId == 0
								 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
								 from pin in pins.DefaultIfEmpty()
								 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
								 orderby (pinned ? pin.Id : 0) descending
								 orderby message.LastReplyPosted descending
								 select message.Id;
			}
			else {
				messageIdQuery = from message in messageQuery
								 where message.ParentId == 0
								 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
								 from pin in pins.DefaultIfEmpty()
								 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
								 where !pinned
								 where message.LastReplyPosted < afterTarget
								 orderby message.LastReplyPosted descending
								 select message.Id;
			}

			var messageBoardsQuery = from messageId in messageIdQuery
									 join messageBoard in DbContext.MessageBoards on messageId equals messageBoard.MessageId into boards
									 from messageBoard in boards.DefaultIfEmpty()
									 select new {
										 MessageId = messageId,
										 BoardId = messageBoard == null ? -1 : messageBoard.BoardId
									 };

			var messageIds = new List<int>();
			var attempts = 0;

			foreach (var messageId in messageIdQuery) {
				if (!UserContext.IsAdmin) {
					var forbidden = messageBoardsQuery.Where(mb => mb.MessageId == messageId).Select(mb => mb.BoardId).Intersect(forbiddenBoardIds).Any();

					if (forbidden) {
						if (attempts++ > 100)
							break;

						continue;
					}
				}

				messageIds.Add(messageId);

				if (messageIds.Count == take)
					break;
			}

			return messageIds;
		}

		List<ItemModels.MessagePreview> GetTopicPreviews(List<int> messageIds) {
			var messageRecordQuery = from message in DbContext.Messages
									 where message.ParentId == 0 && messageIds.Contains(message.Id)
									 join reply in DbContext.Messages on message.LastReplyId equals reply.Id into replies
									 from reply in replies.DefaultIfEmpty()
									 join replyPostedBy in DbContext.Users on message.LastReplyById equals replyPostedBy.Id
									 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
									 from pin in pins.DefaultIfEmpty()
									 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
									 orderby (pinned ? pin.Id : 0) descending, message.LastReplyPosted descending
									 select new ItemModels.MessagePreview {
										 Id = message.Id,
										 ShortPreview = message.ShortPreview,
										 LastReplyId = message.LastReplyId == 0 ? message.Id : message.LastReplyId,
										 LastReplyById = message.LastReplyById,
										 LastReplyByName = replyPostedBy.DisplayName,
										 LastReplyPostedDT = message.LastReplyPosted,
										 LastReplyPreview = reply.ShortPreview,
										 Views = message.ViewCount,
										 Replies = message.ReplyCount,
										 Pinned = pinned
									 };

			var messages = messageRecordQuery.ToList();

			var participation = new List<DataModels.Participant>();
			var viewLogs = new List<DataModels.ViewLog>();
			var take = Settings.MessagesPerPage();
			var historyTimeLimit = Settings.HistoryTimeLimit();

			if (UserContext.IsAuthenticated) {
				participation = DbContext.Participants.Where(r => r.UserId == UserContext.ApplicationUser.Id).ToList();
				viewLogs = DbContext.ViewLogs.Where(r => r.LogTime >= historyTimeLimit && r.UserId == UserContext.ApplicationUser.Id).ToList();
			}

			foreach (var message in messages) {
				message.Pages = Convert.ToInt32(Math.Ceiling(1.0 * message.Replies / take));
				message.LastReplyPosted = message.LastReplyPostedDT.ToPassedTimeString();

				if (message.LastReplyPostedDT > historyTimeLimit)
					TopicUnreadLevel(message, participation, viewLogs);
			}

			return messages;
		}

		void TopicUnreadLevel(ItemModels.MessagePreview message, List<DataModels.Participant> participation, List<DataModels.ViewLog> viewLogs) {
			var unread = 1;

			if (UserContext.IsAuthenticated) {
				foreach (var viewLog in viewLogs) {
					switch (viewLog.TargetType) {
						case EViewLogTargetType.All:
							if (viewLog.LogTime >= message.LastReplyPostedDT)
								unread = 0;
							break;

						case EViewLogTargetType.Message:
							if (viewLog.TargetId == message.Id && viewLog.LogTime >= message.LastReplyPostedDT)
								unread = 0;
							break;
					}
				}
			}

			if (unread == 1 && participation.Any(r => r.MessageId == message.Id))
				unread = 2;

			message.Unread = unread;
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

		PageModels.TopicDisplayPage GetRedirectViewModel(int messageId, int parentMessageId, List<int> messageIds) {
			var viewModel = new PageModels.TopicDisplayPage();

			if (parentMessageId == 0)
				parentMessageId = messageId;

			var routeValues = new {
				id = parentMessageId,
				pageId = GetMessagePage(messageId, messageIds),
				target = messageId
			};

			viewModel.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;

			return viewModel;
		}

		PageModels.TopicDisplayPage GetMigrationRedirectViewModel(int messageId) {
			return new PageModels.TopicDisplayPage {
				RedirectPath = UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = messageId })
			};
		}

		int GetMessagePage(int messageId, List<int> messageIds) {
			var index = (double) messageIds.FindIndex(id => id == messageId);
			index++;

			var messagesPerPage = Settings.MessagesPerPage();
			return Convert.ToInt32(Math.Ceiling(index / messagesPerPage));
		}

		List<DataModels.ViewLog> GetUserViewLogs() {
			if (!UserContext.IsAuthenticated)
				return new List<DataModels.ViewLog>();

			var historyTimeLimit = Settings.HistoryTimeLimit();
			var viewLogs = DbContext.ViewLogs.Where(r => r.UserId == UserContext.ApplicationUser.Id).ToList();

			var expiredViewLogs = viewLogs.Where(r =>
				r.TargetType == EViewLogTargetType.All
				&& r.LogTime <= historyTimeLimit
			).ToList();

			if (expiredViewLogs.Any()) {
				foreach (var viewLog in expiredViewLogs) {
					viewLogs.Remove(viewLog);
					DbContext.ViewLogs.Remove(viewLog);
				}

				DbContext.SaveChanges();
			}

			return viewLogs;
		}
	}
}