using Forum.Contexts;
using Forum.Enums;
using Forum.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using ServiceModels = Models.ServiceModels;

	public class TopicRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		MessageRepository MessageRepository { get; }
		NotificationRepository NotificationRepository { get; }
		PinRepository PinRepository { get; }
		RoleRepository RoleRepository { get; }
		SmileyRepository SmileyRepository { get; }

		IUrlHelper UrlHelper { get; }

		public TopicRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			MessageRepository messageRepository,
			PinRepository pinRepository,
			NotificationRepository notificationRepository,
			RoleRepository roleRepository,
			SmileyRepository smileyRepository,
			AccountRepository accountRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			BoardRepository = boardRepository;
			MessageRepository = messageRepository;
			NotificationRepository = notificationRepository;
			PinRepository = pinRepository;
			RoleRepository = roleRepository;
			SmileyRepository = smileyRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public ServiceModels.ServiceResponse GetLatest(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.Find(messageId);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			if (record.ParentId > 0) {
				record = DbContext.Messages.Find(record.ParentId);
			}

			if (!UserContext.IsAuthenticated) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Topics.Display), nameof(Controllers.Topics), new { id = record.LastReplyId });
				return serviceResponse;
			}

			var historyTimeLimit = DateTime.Now.AddDays(-14);
			var latestViewTime = historyTimeLimit;

			foreach (var viewLog in UserContext.ViewLogs) {
				switch (viewLog.TargetType) {
					case EViewLogTargetType.All:
						if (viewLog.LogTime >= latestViewTime) {
							latestViewTime = viewLog.LogTime;
						}

						break;

					case EViewLogTargetType.Message:
						if (viewLog.TargetId == record.Id && viewLog.LogTime >= latestViewTime) {
							latestViewTime = viewLog.LogTime;
						}

						break;
				}
			}

			var messageIdQuery = from message in DbContext.Messages
								 where message.Id == record.Id || message.ParentId == record.Id
								 where message.TimePosted > latestViewTime
								 select message.Id;

			var latestMessageId = messageIdQuery.FirstOrDefault();

			if (latestMessageId == 0) {
				latestMessageId = record.LastReplyId;
			}

			if (latestMessageId == 0) {
				latestMessageId = record.Id;
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Topics.Display), nameof(Controllers.Topics), new { id = latestMessageId });

			return serviceResponse;
		}

		public async Task<List<ItemModels.MessagePreview>> GetPreviews(int boardId, int page, int unread) {
			var participation = new List<DataModels.Participant>();
			var historyTimeLimit = DateTime.Now.AddDays(-14);

			if (UserContext.IsAuthenticated) {
				participation = DbContext.Participants.Where(r => r.UserId == UserContext.ApplicationUser.Id).ToList();
			}

			var sortedMessageIds = await GetIndexIds(boardId, page, unread, historyTimeLimit, participation);

			var messageQuery = from message in DbContext.Messages
							   where sortedMessageIds.Contains(message.Id)
							   select new {
								   message.Id,
								   message.ShortPreview,
								   message.ViewCount,
								   message.ReplyCount,
								   message.TimePosted,
								   message.PostedById,
								   message.LastReplyId,
								   message.LastReplyById,
								   message.LastReplyPosted
							   };

			var messages = messageQuery.ToList();
			var users = await AccountRepository.Records();
			var pins = await PinRepository.Records();

			var messagePreviews = new List<ItemModels.MessagePreview>();
			var today = DateTime.Now.Date;

			foreach (var messageId in sortedMessageIds) {
				var message = messages.First(item => item.Id == messageId);
				var postedBy = users.First(r => r.Id == message.PostedById);

				var messagePreview = new ItemModels.MessagePreview {
					Id = message.Id,
					ShortPreview = string.IsNullOrEmpty(message.ShortPreview.Trim()) ? "No subject" : message.ShortPreview,
					Views = message.ViewCount,
					Replies = message.ReplyCount,
					Pages = Convert.ToInt32(Math.Ceiling(1.0 * message.ReplyCount / UserContext.ApplicationUser.MessagesPerPage)),
					LastReplyId = message.Id,
					Popular = message.ReplyCount > UserContext.ApplicationUser.PopularityLimit,
					Pinned = pins.Any(item => item.MessageId == message.Id),
					TimePosted = message.TimePosted,
					PostedById = message.PostedById,
					PostedByName = postedBy.DisplayName,
					PostedByBirthday = today == new DateTime(today.Year, postedBy.Birthday.Month, postedBy.Birthday.Day).Date
				};

				messagePreviews.Add(messagePreview);
				
				var lastMessageTime = message.TimePosted;

				if (message.LastReplyId != 0) {
					var lastReply = (from item in DbContext.Messages
											where item.Id == message.LastReplyId
											select new {
												item.ShortPreview
											}).FirstOrDefault();

					if (lastReply != null) {
						var lastReplyBy = users.First(r => r.Id == message.LastReplyById);

						messagePreview.LastReplyId = message.LastReplyId;
						messagePreview.LastReplyPreview = lastReply.ShortPreview;
						messagePreview.LastReplyByName = lastReplyBy.DisplayName;
						messagePreview.LastReplyById = message.LastReplyById;
						messagePreview.LastReplyByBirthday = today.Date == new DateTime(today.Year, lastReplyBy.Birthday.Month, lastReplyBy.Birthday.Day).Date;
						messagePreview.LastReplyPosted = message.LastReplyPosted;
						lastMessageTime = message.LastReplyPosted;
					}
				}

				if (lastMessageTime > historyTimeLimit) {
					messagePreview.Unread = GetUnreadLevel(message.Id, lastMessageTime, participation);
				}
			}

			return messagePreviews;
		}

		public async Task<List<int>> GetIndexIds(int boardId, int page, int unreadFilter, DateTime historyTimeLimit, List<DataModels.Participant> participation) {
			var take = UserContext.ApplicationUser.TopicsPerPage;
			var skip = (page - 1) * take;

			var messageQuery = from message in DbContext.Messages
							   where message.ParentId == 0
							   select new {
								   message.Id,
								   message.LastReplyPosted
							   };

			if (boardId > 0) {
				messageQuery = from message in DbContext.Messages
							   join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
							   where message.ParentId == 0
							   where messageBoard.BoardId == boardId
							   select new {
								   message.Id,
								   message.LastReplyPosted
							   };
			}

			if (unreadFilter > 0) {
				messageQuery = messageQuery.Where(m => m.LastReplyPosted > historyTimeLimit);
			}

			var pins = await PinRepository.Records();
			var pinnedTopicIds = pins.Select(item => item.MessageId).ToList();

			var sortedMessageQuery = from message in messageQuery
									 let pinned = pinnedTopicIds.Contains(message.Id)
									 orderby message.LastReplyPosted descending
									 orderby pinned descending
									 select new {
										 message.Id,
										 message.LastReplyPosted
									 };

			var messageIds = new List<int>();
			var attempts = 0;
			var skipped = 0;

			foreach (var message in sortedMessageQuery) {
				if (!await BoardRepository.CanAccess(message.Id)) {
					if (attempts++ > 100) {
						break;
					}

					continue;
				}

				var unreadLevel = unreadFilter == 0 ? 0 : GetUnreadLevel(message.Id, message.LastReplyPosted, participation);

				if (unreadLevel < unreadFilter) {
					if (attempts++ > 100) {
						break;
					}

					continue;
				}

				if (skipped++ < skip) {
					continue;
				}

				messageIds.Add(message.Id);

				if (messageIds.Count == take) {
					break;
				}
			}

			return messageIds;
		}
		
		public int GetUnreadLevel(int messageId, DateTime lastMessageTime, List<DataModels.Participant> participation) {
			var unread = 1;

			if (UserContext.IsAuthenticated) {
				foreach (var viewLog in UserContext.ViewLogs.Where(item => item.LogTime >= lastMessageTime)) {
					switch (viewLog.TargetType) {
						case EViewLogTargetType.All:
							unread = 0;
							break;

						case EViewLogTargetType.Message:
							if (viewLog.TargetId == messageId) {
								unread = 0;
							}

							break;
					}

					// Exit the loop early if we already know it's been read.
					if (unread == 0) {
						break;
					}
				}
			}

			if (unread == 1 && participation.Any(r => r.MessageId == messageId)) {
				unread = 2;
			}

			return unread;
		}

		public ServiceModels.ServiceResponse Pin(int messageId) {
			var record = DbContext.Messages.Find(messageId);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			if (record.ParentId > 0) {
				messageId = record.ParentId;
			}

			var existingRecord = DbContext.Pins.FirstOrDefault(p => p.MessageId == messageId && p.UserId == UserContext.ApplicationUser.Id);

			if (existingRecord is null) {
				var pinRecord = new DataModels.Pin {
					MessageId = messageId,
					Time = DateTime.Now,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.Pins.Add(pinRecord);
			}
			else {
				DbContext.Pins.Remove(existingRecord);
			}

			DbContext.SaveChanges();

			return new ServiceModels.ServiceResponse();
		}

		public ServiceModels.ServiceResponse MarkAllRead() {
			if (UserContext.ViewLogs.Any()) {
				foreach (var viewLog in UserContext.ViewLogs) {
					DbContext.Remove(viewLog);
				}

				DbContext.SaveChanges();
			}

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				UserId = UserContext.ApplicationUser.Id,
				LogTime = DateTime.Now,
				TargetType = EViewLogTargetType.All
			});

			DbContext.SaveChanges();

			return new ServiceModels.ServiceResponse {
				RedirectPath = "/"
			};
		}

		public ServiceModels.ServiceResponse MarkUnread(int messageId) {
			var record = DbContext.Messages.Find(messageId);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			if (record.ParentId > 0) {
				messageId = record.ParentId;
			}

			var viewLogs = UserContext.ViewLogs.Where(item => item.TargetId == messageId && item.TargetType == EViewLogTargetType.Message).ToList();

			if (viewLogs.Any()) {
				foreach (var viewLog in viewLogs) {
					DbContext.Remove(viewLog);
				}

				DbContext.SaveChanges();
			}

			return new ServiceModels.ServiceResponse {
				RedirectPath = "/"
			};
		}

		public async Task Toggle(InputModels.ToggleBoardInput input) {
			var messageRecord = await DbContext.Messages.FindAsync(input.MessageId);

			if (messageRecord is null) {
				throw new HttpNotFoundError();
			}

			if (!(await BoardRepository.Records()).Any(r => r.Id == input.BoardId)) {
				throw new HttpNotFoundError();
			}

			var messageId = input.MessageId;

			if (messageRecord.ParentId > 0) {
				messageId = messageRecord.ParentId;
			}

			var boardId = input.BoardId;

			var existingRecord = await DbContext.MessageBoards.FirstOrDefaultAsync(p => p.MessageId == messageId && p.BoardId == boardId);

			if (existingRecord is null) {
				var messageBoardRecord = new DataModels.MessageBoard {
					MessageId = messageId,
					BoardId = boardId,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.MessageBoards.Add(messageBoardRecord);
			}
			else {
				DbContext.MessageBoards.Remove(existingRecord);
			}

			await DbContext.SaveChangesAsync();
		}

		public void MarkRead(int topicId, DateTime latestMessageTime, List<int> pageMessageIds) {
			if (!UserContext.IsAuthenticated) {
				return;
			}

			var viewLogs = DbContext.ViewLogs.Where(v =>
				v.UserId == UserContext.ApplicationUser.Id
				&& (v.TargetType == EViewLogTargetType.All || (v.TargetType == EViewLogTargetType.Message && v.TargetId == topicId))
			).ToList();

			DateTime latestTime;

			if (viewLogs.Any()) {
				var latestViewLogTime = viewLogs.Max(r => r.LogTime);
				latestTime = latestViewLogTime > latestMessageTime ? latestViewLogTime : latestMessageTime;
			}
			else {
				latestTime = latestMessageTime;
			}

			latestTime.AddSeconds(1);

			var existingLogs = viewLogs.Where(r => r.TargetType == EViewLogTargetType.Message);

			foreach (var viewLog in existingLogs) {
				DbContext.ViewLogs.Remove(viewLog);
			}

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				LogTime = latestTime,
				TargetId = topicId,
				TargetType = EViewLogTargetType.Message,
				UserId = UserContext.ApplicationUser.Id
			});

			foreach (var messageId in pageMessageIds) {
				foreach (var notification in DbContext.Notifications.Where(item => item.UserId == UserContext.ApplicationUser.Id && item.Type != ENotificationType.Thought && item.MessageId == messageId)) {
					notification.Unread = false;
					DbContext.Update(notification);
				}
			}

			try {
				DbContext.SaveChanges();
			}
			// see - https://docs.microsoft.com/en-us/ef/core/saving/concurrency
			// The user probably refreshed several times in a row.
			catch (DbUpdateConcurrencyException) { }
		}

		public ServiceModels.ServiceResponse Merge(int sourceId, int targetId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var sourceRecord = DbContext.Messages.FirstOrDefault(item => item.Id == sourceId);

			if (sourceRecord is null) {
				serviceResponse.Error("Source record not found");
			}

			var targetRecord = DbContext.Messages.FirstOrDefault(item => item.Id == targetId);

			if (targetRecord is null) {
				serviceResponse.Error("Target record not found");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			if (sourceRecord.TimePosted > targetRecord.TimePosted) {
				Merge(sourceRecord, targetRecord);
			}
			else {
				Merge(targetRecord, sourceRecord);
			}

			return serviceResponse;
		}

		public void Merge(DataModels.Message sourceMessage, DataModels.Message targetMessage) {
			UpdateMessagesParentId(sourceMessage, targetMessage);
			MessageRepository.RecountRepliesForTopic(targetMessage);
			RemoveTopicParticipants(sourceMessage, targetMessage);
			MessageRepository.RebuildParticipantsForTopic(targetMessage.Id);
			RemoveTopicViewlogs(sourceMessage, targetMessage);
			RemoveTopicPins(sourceMessage);
			RemoveTopicMessageBoards(sourceMessage);
		}

		public void UpdateMessagesParentId(DataModels.Message sourceMessage, DataModels.Message targetMessage) {
			var sourceMessages = DbContext.Messages.Where(item => item.Id == sourceMessage.Id || item.ParentId == sourceMessage.Id).ToList();

			foreach (var message in sourceMessages) {
				message.ParentId = targetMessage.Id;
				DbContext.Update(message);
			}

			DbContext.SaveChanges();
		}

		public void RemoveTopicParticipants(DataModels.Message sourceMessage, DataModels.Message targetMessage) {
			var records = DbContext.Participants.Where(item => item.MessageId == sourceMessage.Id).ToList();
			DbContext.RemoveRange(records);
			DbContext.SaveChanges();
		}

		public void RemoveTopicViewlogs(DataModels.Message sourceMessage, DataModels.Message targetMessage) {
			var records = DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Message && (item.TargetId == sourceMessage.Id || item.TargetId == targetMessage.Id)).ToList();
			DbContext.RemoveRange(records);
			DbContext.SaveChanges();
		}

		public void RemoveTopicPins(DataModels.Message sourceMessage) {
			var records = DbContext.Pins.Where(item => item.MessageId == sourceMessage.Id);
			DbContext.RemoveRange(records);
			DbContext.SaveChanges();
		}

		public void RemoveTopicMessageBoards(DataModels.Message sourceMessage) {
			var records = DbContext.MessageBoards.Where(item => item.MessageId == sourceMessage.Id);
			DbContext.RemoveRange(records);
			DbContext.SaveChanges();
		}
	}
}
