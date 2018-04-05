using Forum3.Contexts;
using Forum3.Extensions;
using Forum3.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Processes.Topics {
	using DataModels = Models.DataModels;
	using ItemModels = Models.ViewModels.Topics.Items;

	public class LoadTopicPreview {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		SettingsRepository Settings { get; }
		TopicUnreadLevelCalculator TopicUnreadLevelCalculator { get; }

		public LoadTopicPreview(
			ApplicationDbContext dbContext,
			UserContext userContext,
			SettingsRepository settingsRepository,
			TopicUnreadLevelCalculator topicUnreadLevelCalculator
		) {
			DbContext = dbContext;
			UserContext = userContext;
			Settings = settingsRepository;
			TopicUnreadLevelCalculator = topicUnreadLevelCalculator;
		}

		public List<ItemModels.MessagePreview> Execute(int boardId, long after, int unread) {
			var participation = new List<DataModels.Participant>();
			var viewLogs = new List<DataModels.ViewLog>();
			var historyTimeLimit = Settings.HistoryTimeLimit();

			if (UserContext.IsAuthenticated) {
				participation = DbContext.Participants.Where(r => r.UserId == UserContext.ApplicationUser.Id).ToList();
				viewLogs = DbContext.ViewLogs.Where(r => r.LogTime >= historyTimeLimit && r.UserId == UserContext.ApplicationUser.Id).ToList();
			}

			var messageIds = GetIndexIds(boardId, after, unread, historyTimeLimit, participation, viewLogs);

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

			var take = Settings.MessagesPerPage();

			foreach (var message in messages) {
				message.Pages = Convert.ToInt32(Math.Ceiling(1.0 * message.Replies / take));
				message.LastReplyPosted = message.LastReplyPostedDT.ToPassedTimeString();

				if (message.LastReplyPostedDT > historyTimeLimit)
					message.Unread = TopicUnreadLevelCalculator.Execute(message.Id, message.LastReplyPostedDT, participation, viewLogs);
			}

			return messages;
		}

		List<int> GetIndexIds(int boardId, long after, int unreadFilter, DateTime historyTimeLimit, List<DataModels.Participant> participation, List<DataModels.ViewLog> viewLogs) {
			var take = Settings.TopicsPerPage();

			var messageQuery = from message in DbContext.Messages
							   where message.ParentId == 0
							   select new {
								   message.Id,
								   message.ParentId,
								   message.LastReplyPosted
							   };

			if (boardId > 0) {
				messageQuery = from message in DbContext.Messages
							   join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
							   where message.ParentId == 0
							   where messageBoard.BoardId == boardId
							   select new {
								   message.Id,
								   message.ParentId,
								   message.LastReplyPosted
							   };
			}

			var afterTarget = new DateTime(after);

			if (unreadFilter > 0) {
				var timeFilter = historyTimeLimit > afterTarget ? historyTimeLimit : afterTarget;
				messageQuery = messageQuery.Where(m => m.LastReplyPosted > timeFilter);
			}
			else if (afterTarget != default(DateTime))
				messageQuery = messageQuery.Where(m => m.LastReplyPosted < afterTarget);

			var sortedMessageQuery = from message in messageQuery
									 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
									 from pin in pins.DefaultIfEmpty()
									 let pinned = pin != null && pin.UserId == UserContext.ApplicationUser.Id
									 orderby message.LastReplyPosted descending
									 orderby (pinned ? pin.Id : 0) descending
									 select new {
										 message.Id,
										 message.LastReplyPosted
									 };

			var messageBoardsQuery = from message in sortedMessageQuery
									 join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId into boards
									 from messageBoard in boards.DefaultIfEmpty()
									 select new {
										 MessageId = message.Id,
										 BoardId = messageBoard == null ? -1 : messageBoard.BoardId
									 };

			var forbiddenBoardIdsQuery = from role in DbContext.Roles
										 join board in DbContext.BoardRoles on role.Id equals board.RoleId
										 where !UserContext.Roles.Contains(role.Id)
										 select board.BoardId;

			var forbiddenBoardIds = forbiddenBoardIdsQuery.ToList();

			var messageIds = new List<int>();
			var attempts = 0;

			foreach (var message in sortedMessageQuery) {
				if (AccessDenied(message.Id, forbiddenBoardIds)) {
					if (attempts++ > 100)
						break;

					continue;
				}

				var unreadLevel = unreadFilter == 0 ? 0 : TopicUnreadLevelCalculator.Execute(message.Id, message.LastReplyPosted, participation, viewLogs);

				if (unreadLevel < unreadFilter) {
					if (attempts++ > 100)
						break;

					continue;
				}

				messageIds.Add(message.Id);

				if (messageIds.Count == take)
					break;
			}

			return messageIds;
		}

		bool AccessDenied(int messageId, List<int> forbiddenBoardIds) {
			if (UserContext.IsAdmin)
				return false;

			var messageBoards = DbContext.MessageBoards.Where(mb => mb.MessageId == messageId).Select(mb => mb.BoardId);

			return messageBoards.Any() && messageBoards.Intersect(forbiddenBoardIds).Any();
		}
	}
}