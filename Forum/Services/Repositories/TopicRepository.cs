using Forum.Controllers;
using Forum.Models.Errors;
using Forum.Models.Options;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using ControllerModels = Models.ControllerModels;
	using DataModels = Models.DataModels;
	using HubModels = Models.HubModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.Topics;

	public class TopicRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		BookmarkRepository BookmarkRepository { get; }
		MessageRepository MessageRepository { get; }
		IHubContext<ForumHub> ForumHub { get; }
		IUrlHelper UrlHelper { get; }

		public TopicRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			BookmarkRepository bookmarkRepository,
			MessageRepository messageRepository,
			AccountRepository accountRepository,
			IHubContext<ForumHub> forumHub,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			BoardRepository = boardRepository;
			BookmarkRepository = bookmarkRepository;
			MessageRepository = messageRepository;
			ForumHub = forumHub;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<int> GetTopicTargetMessageId(int topicId) {
			var topic = DbContext.Topics.Find(topicId);

			if (topic is null || topic.Deleted) {
				throw new HttpNotFoundError();
			}

			var latestMessageId = topic.LastMessageId;

			if (UserContext.IsAuthenticated) {
				var historyTimeLimit = DateTime.Now.AddDays(-14);
				var latestViewTime = historyTimeLimit;

				foreach (var viewLog in UserContext.ViewLogs) {
					switch (viewLog.TargetType) {
						case EViewLogTargetType.All:
							if (viewLog.LogTime >= latestViewTime) {
								latestViewTime = viewLog.LogTime;
							}

							break;

						case EViewLogTargetType.Topic:
							if (viewLog.TargetId == topic.Id && viewLog.LogTime >= latestViewTime) {
								latestViewTime = viewLog.LogTime;
							}

							break;
					}
				}

				var messageIdQuery = from message in DbContext.Messages
									 where message.TopicId == topic.Id
									 where message.TimePosted > latestViewTime
									 where !message.Deleted
									 select message.Id;

				if (messageIdQuery.Any()) {
					latestMessageId = await messageIdQuery.FirstAsync();
				}
			}

			return latestMessageId;
		}

		public async Task<List<ViewModels.TopicPreview>> GetPreviews(List<int> topicIds) {
			var topicsQuery = from topic in DbContext.Topics
							  where topicIds.Contains(topic.Id)
							  where !topic.Deleted
							  select topic;

			var topics = await topicsQuery.ToListAsync();

			var topicBoardsQuery = from topicBoard in DbContext.TopicBoards
								   where topicIds.Contains(topicBoard.TopicId)
								   select new {
									   topicBoard.BoardId,
									   topicBoard.TopicId
								   };

			var topicBoards = await topicBoardsQuery.ToListAsync();

			var users = await AccountRepository.Records();

			var topicPreviews = new List<ViewModels.TopicPreview>();
			var today = DateTime.Now.Date;

			var boards = await BoardRepository.Records();

			foreach (var topicId in topicIds) {
				var topic = topics.First(item => item.Id == topicId);
				var firstMessagePostedBy = users.First(r => r.Id == topic.FirstMessagePostedById);
				var lastMessagePostedBy = users.FirstOrDefault(r => r.Id == topic.LastMessagePostedById);

				var topicPreview = new ViewModels.TopicPreview {
					Id = topic.Id,
					Pinned = topic.Pinned,
					ViewCount = topic.ViewCount,
					ReplyCount = topic.ReplyCount,
					Popular = topic.ReplyCount > UserContext.ApplicationUser.PopularityLimit,
					Pages = Convert.ToInt32(Math.Ceiling(1.0 * topic.ReplyCount / UserContext.ApplicationUser.MessagesPerPage)),
					FirstMessageId = topic.FirstMessageId,
					FirstMessageTimePosted = topic.FirstMessageTimePosted,
					FirstMessagePostedById = topic.FirstMessagePostedById,
					FirstMessagePostedByName = firstMessagePostedBy?.DecoratedName ?? "User",
					FirstMessagePostedByBirthday = today == new DateTime(today.Year, firstMessagePostedBy.Birthday.Month, firstMessagePostedBy.Birthday.Day).Date,
					FirstMessageShortPreview = string.IsNullOrEmpty(topic.FirstMessageShortPreview.Trim()) ? "No subject" : topic.FirstMessageShortPreview,
					LastMessageId = topic.Id,
					LastMessageTimePosted = topic.LastMessageTimePosted,
					LastMessagePostedById = topic.LastMessagePostedById,
					LastMessagePostedByName = lastMessagePostedBy?.DecoratedName ?? "User",
					LastMessagePostedByBirthday = today == new DateTime(today.Year, firstMessagePostedBy.Birthday.Month, firstMessagePostedBy.Birthday.Day).Date,
				};

				topicPreviews.Add(topicPreview);

				var historyTimeLimit = DateTime.Now.AddDays(-14);

				if (topic.LastMessageTimePosted > historyTimeLimit) {
					topicPreview.Unread = GetUnreadLevel(topic.Id, topic.LastMessageTimePosted);
				}

				var topicPreviewBoardsQuery = from topicBoard in topicBoards
											  where topicBoard.TopicId == topic.Id
											  join board in boards on topicBoard.BoardId equals board.Id
											  orderby board.DisplayOrder
											  select new Models.ViewModels.Boards.IndexBoard {
												  Id = board.Id,
												  Name = board.Name,
												  Description = board.Description,
												  DisplayOrder = board.DisplayOrder,
											  };

				topicPreview.Boards = topicPreviewBoardsQuery.ToList();

				var topicEventQuery = from topicEvent in DbContext.Events
									  where topicEvent.TopicId == topic.Id
									  select topicEvent;

				topicPreview.Event = await topicEventQuery.AnyAsync();
			}

			return topicPreviews;
		}

		public async Task<List<int>> GetIndexIds(int boardId, int page, int unreadFilter) {
			var take = UserContext.ApplicationUser.TopicsPerPage;
			var skip = (page - 1) * take;
			var historyTimeLimit = DateTime.Now.AddDays(-14);

			var topicQuery = from topic in DbContext.Topics
							 where !topic.Deleted
							 select new {
								 topic.Id,
								 topic.LastMessageTimePosted,
								 topic.Pinned
							 };

			if (boardId > 0) {
				topicQuery = from topic in DbContext.Topics
							 join topicBoard in DbContext.TopicBoards on topic.Id equals topicBoard.TopicId
							 where topicBoard.BoardId == boardId
							 where !topic.Deleted
							 select new {
								 topic.Id,
								 topic.LastMessageTimePosted,
								 topic.Pinned
							 };
			}

			if (unreadFilter > 0) {
				topicQuery = topicQuery.Where(m => m.LastMessageTimePosted > historyTimeLimit);
			}

			var sortedTopicQuery = from topic in topicQuery
								   orderby topic.LastMessageTimePosted descending
								   orderby topic.Pinned descending
								   select new {
									   topic.Id,
									   topic.LastMessageTimePosted
								   };

			var topicIds = new List<int>();
			var attempts = 0;
			var skipped = 0;

			foreach (var topic in sortedTopicQuery) {
				if (!await BoardRepository.CanAccess(topic.Id)) {
					if (attempts++ > 100) {
						break;
					}

					continue;
				}

				var unreadLevel = unreadFilter == 0 ? 0 : GetUnreadLevel(topic.Id, topic.LastMessageTimePosted);

				if (unreadLevel < unreadFilter) {
					if (attempts++ > 100) {
						break;
					}

					continue;
				}

				if (skipped++ < skip) {
					continue;
				}

				topicIds.Add(topic.Id);

				if (topicIds.Count == take) {
					break;
				}
			}

			return topicIds;
		}

		public int GetUnreadLevel(int topicId, DateTime lastMessageTime) {
			var unread = 1;

			if (UserContext.IsAuthenticated) {
				foreach (var viewLog in UserContext.ViewLogs.Where(item => item.LogTime >= lastMessageTime)) {
					switch (viewLog.TargetType) {
						case EViewLogTargetType.All:
							unread = 0;
							break;

						case EViewLogTargetType.Topic:
							if (viewLog.TargetId == topicId) {
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

			if (unread == 1 && DbContext.Participants.Any(r => r.TopicId == topicId && r.UserId == UserContext.ApplicationUser.Id)) {
				unread = 2;
			}

			return unread;
		}

		public async Task<ControllerModels.Topics.CreateTopicResult> CreateTopic(ControllerModels.Topics.CreateTopicInput input) {
			var result = new ControllerModels.Topics.CreateTopicResult();

			var processedMessage = await MessageRepository.ProcessMessageInput(input.Body);
			result.Errors = processedMessage.Errors;

			if (!result.Errors.Any()) {
				var message = await MessageRepository.CreateMessageRecord(processedMessage);

				var topic = new DataModels.Topic {
					FirstMessageId = message.Id,
					FirstMessagePostedById = message.PostedById,
					FirstMessageTimePosted = message.TimePosted,
					FirstMessageShortPreview = message.ShortPreview,
					LastMessageId = message.Id,
					LastMessagePostedById = message.PostedById,
					LastMessageTimePosted = message.TimePosted,
					LastMessageShortPreview = message.ShortPreview,
				};

				DbContext.Topics.Add(topic);
				await DbContext.SaveChangesAsync();

				if (!(input.Start is null) && !(input.End is null)) {
					try {
						await AddEvent(new ControllerModels.Topics.EditEventInput {
							TopicId = topic.Id,
							Start = input.Start,
							End = input.End,
							AllDay = input.AllDay
						});
					}
					catch (ArgumentException e) {
						result.Errors.Add(e.ParamName, e.Message);
						DbContext.Topics.Remove(topic);
						await DbContext.SaveChangesAsync();
					}
				}

				if (!result.Errors.Any()) {
					message.TopicId = topic.Id;
					DbContext.Update(message);

					MessageRepository.UpdateTopicParticipation(topic.Id, UserContext.ApplicationUser.Id, message.TimePosted);

					var boards = await BoardRepository.Records();

					foreach (var selectedBoard in input.SelectedBoards) {
						var board = boards.FirstOrDefault(item => item.Id == selectedBoard);

						if (board != null) {
							DbContext.TopicBoards.Add(new DataModels.TopicBoard {
								TopicId = topic.Id,
								BoardId = board.Id,
								TimeAdded = DateTime.Now,
								UserId = UserContext.ApplicationUser.Id
							});
						}
					}

					await DbContext.SaveChangesAsync();

					await ForumHub.Clients.All.SendAsync("new-topic", new HubModels.Message {
						TopicId = topic.Id,
						MessageId = message.Id
					});

					result.TopicId = topic.Id;
					result.MessageId = message.Id;
				}
			}

			return result;
		}

		public async Task<ControllerModels.Topics.EditEventResult> AddEvent(ControllerModels.Topics.EditEventInput input) {
			var result = new ControllerModels.Topics.EditEventResult();

			var topicRecord = DbContext.Topics.Find(input.TopicId);

			if (topicRecord is null) {
				throw new HttpNotFoundError();
			}

			if (input.Start is null) {
				throw new ArgumentException("Parameter cannot be null.", nameof(input.Start));
			}

			if (input.End is null) {
				throw new ArgumentException("Parameter cannot be null.", nameof(input.End));
			}

			result.TopicId = topicRecord.Id;
			result.MessageId = topicRecord.FirstMessageId;

			var eventRecord = new DataModels.Event {
				TopicId = input.TopicId,
				Start = (DateTime)input.Start,
				End = (DateTime)input.End,
				AllDay = input.AllDay
			};

			DbContext.Add(eventRecord);

			await DbContext.SaveChangesAsync();

			// TODO: Send topic-updated result to hub if topic ID != null.

			return result;
		}

		public async Task<ServiceModels.ServiceResponse> Pin(int topicId) {
			var record = DbContext.Topics.Find(topicId);

			if (record is null || record.Deleted) {
				throw new HttpNotFoundError();
			}

			record.Pinned = !record.Pinned;

			await DbContext.SaveChangesAsync();

			return new ServiceModels.ServiceResponse();
		}

		public async Task Bookmark(int topicId) {
			var record = await DbContext.Topics.FindAsync(topicId);

			if (record is null || record.Deleted) {
				throw new HttpNotFoundError();
			}

			var bookmarks = await BookmarkRepository.Records();
			var existingBookmarkRecord = bookmarks.FirstOrDefault(p => p.TopicId == topicId);

			if (existingBookmarkRecord is null) {
				var bookmarkRecord = new DataModels.Bookmark {
					TopicId = topicId,
					Time = DateTime.Now,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.Bookmarks.Add(bookmarkRecord);
			}
			else {
				DbContext.Bookmarks.Remove(existingBookmarkRecord);
			}

			await DbContext.SaveChangesAsync();
		}

		public async Task<ServiceModels.ServiceResponse> MarkAllRead() {
			if (UserContext.ViewLogs.Any()) {
				DbContext.RemoveRange(UserContext.ViewLogs);
				await DbContext.SaveChangesAsync();
			}

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				UserId = UserContext.ApplicationUser.Id,
				LogTime = DateTime.Now,
				TargetType = EViewLogTargetType.All
			});

			await DbContext.SaveChangesAsync();

			return new ServiceModels.ServiceResponse {
				RedirectPath = "/"
			};
		}

		public async Task<ServiceModels.ServiceResponse> MarkUnread(int topicId) {
			var record = DbContext.Topics.Find(topicId);

			if (record is null || record.Deleted) {
				throw new HttpNotFoundError();
			}

			var viewLogs = UserContext.ViewLogs.Where(item => item.TargetId == topicId && item.TargetType == EViewLogTargetType.Topic).ToList();

			if (viewLogs.Any()) {
				DbContext.RemoveRange(viewLogs);
				await DbContext.SaveChangesAsync();
			}

			return new ServiceModels.ServiceResponse {
				RedirectPath = "/"
			};
		}

		public async Task ToggleBoard(InputModels.ToggleBoardInput input) {
			var topic = await DbContext.Topics.FindAsync(input.TopicId);
			var boards = await BoardRepository.Records();

			if (topic is null || topic.Deleted || !boards.Any(r => r.Id == input.BoardId)) {
				throw new HttpNotFoundError();
			}

			var existingRecords = await DbContext.TopicBoards.Where(p => p.TopicId == input.TopicId && p.BoardId == input.BoardId).ToListAsync();

			if (existingRecords.Count() == 0) {
				var topicBoardRecord = new DataModels.TopicBoard {
					TopicId = input.TopicId,
					BoardId = input.BoardId,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.TopicBoards.Add(topicBoardRecord);
			}
			else {
				DbContext.TopicBoards.RemoveRange(existingRecords);
			}

			await DbContext.SaveChangesAsync();
		}

		public async Task MarkRead(int topicId, DateTime latestMessageTime, List<int> pageMessageIds) {
			if (!UserContext.IsAuthenticated) {
				return;
			}

			var viewLogs = await DbContext.ViewLogs.Where(v =>
				v.UserId == UserContext.ApplicationUser.Id
				&& (v.TargetType == EViewLogTargetType.All || (v.TargetType == EViewLogTargetType.Topic && v.TargetId == topicId))
			).ToListAsync();

			DateTime latestTime;

			if (viewLogs.Any()) {
				var latestViewLogTime = viewLogs.Max(r => r.LogTime);
				latestTime = latestViewLogTime > latestMessageTime ? latestViewLogTime : latestMessageTime;
			}
			else {
				latestTime = latestMessageTime;
			}

			latestTime.AddSeconds(1);

			var existingLogs = viewLogs.Where(r => r.TargetType == EViewLogTargetType.Topic);

			foreach (var viewLog in existingLogs) {
				DbContext.ViewLogs.Remove(viewLog);
			}

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				LogTime = latestTime,
				TargetId = topicId,
				TargetType = EViewLogTargetType.Topic,
				UserId = UserContext.ApplicationUser.Id
			});

			foreach (var messageId in pageMessageIds) {
				// Mark any relevant notifications read EXCEPT about thoughts
				foreach (var notification in DbContext.Notifications.Where(item => item.UserId == UserContext.ApplicationUser.Id && item.Type != ENotificationType.Thought && item.MessageId == messageId)) {
					notification.Unread = false;
					DbContext.Update(notification);
				}
			}

			await DbContext.SaveChangesAsync();
		}

		public async Task<ServiceModels.ServiceResponse> Merge(int sourceId, int targetId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var sourceRecord = DbContext.Topics.FirstOrDefault(item => item.Id == sourceId);

			if (sourceRecord is null || sourceRecord.Deleted) {
				serviceResponse.Error("Source record not found");
			}

			var targetRecord = DbContext.Topics.FirstOrDefault(item => item.Id == targetId);

			if (targetRecord is null || targetRecord.Deleted) {
				serviceResponse.Error("Target record not found");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			if (sourceRecord.FirstMessageTimePosted > targetRecord.FirstMessageTimePosted) {
				await Merge(sourceRecord, targetRecord);
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Latest), nameof(Topics), new { id = targetRecord.Id });
			}
			else {
				await Merge(targetRecord, sourceRecord);
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Latest), nameof(Topics), new { id = sourceRecord.Id });
			}

			return serviceResponse;
		}

		public async Task Merge(DataModels.Topic sourceTopic, DataModels.Topic targetTopic) {
			await UpdateMessagesTopicId(sourceTopic, targetTopic);
			await RebuildTopicReplies(targetTopic);
			await RemoveTopic(sourceTopic);

			await DbContext.SaveChangesAsync();
		}

		public async Task UpdateMessagesTopicId(DataModels.Topic sourceTopic, DataModels.Topic targetTopic) {
			var sourceMessages = await DbContext.Messages.Where(item => item.TopicId == sourceTopic.Id).ToListAsync();

			foreach (var message in sourceMessages) {
				message.TopicId = targetTopic.Id;
				DbContext.Update(message);
			}
		}

		public async Task RemoveTopic(DataModels.Topic topic) {
			await RemoveTopicViewLogs(topic.Id);
			await RemoveTopicBookmarks(topic.Id);
			await RemoveTopicBoards(topic.Id);
			await RemoveTopicEvents(topic.Id);
			await RemoveTopicMessages(topic.Id);

			topic.Deleted = true;
			DbContext.Update(topic);
		}

		public async Task RemoveTopicViewLogs(int topicId) {
			var records = await DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Topic && item.TargetId == topicId).ToListAsync();
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicBookmarks(int topicId) {
			var records = DbContext.Bookmarks.Where(item => item.TopicId == topicId);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicBoards(int topicId) {
			var records = DbContext.TopicBoards.Where(item => item.TopicId == topicId);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicMessages(int topicId) {
			var records = DbContext.Messages.Where(item => item.TopicId == topicId);

			foreach (var record in records) {
				var messageThoughts = await DbContext.MessageThoughts.Where(mt => mt.MessageId == record.Id).ToListAsync();

				foreach (var messageThought in messageThoughts) {
					DbContext.MessageThoughts.Remove(messageThought);
				}

				var notifications = DbContext.Notifications.Where(item => item.MessageId == record.Id).ToList();

				foreach (var notification in notifications) {
					DbContext.Notifications.Remove(notification);
				}

				var quotes = DbContext.Quotes.Where(item => item.MessageId == record.Id).ToList();

				foreach (var quote in quotes) {
					DbContext.Quotes.Remove(quote);
				}

				record.Deleted = true;
			}

			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicEvents(int topicId) {
			var records = DbContext.Events.Where(item => item.TopicId == topicId);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RebuildTopicReplies(DataModels.Topic topic) {
			var messagesQuery = from message in DbContext.Messages
								where message.TopicId == topic.Id && !message.Deleted
								select new {
									message.Id,
									message.TimePosted,
									message.PostedById,
									message.ShortPreview
								};

			var messages = await messagesQuery.ToListAsync();

			var replyCount = messages.Count() - 1;

			if (topic.ReplyCount != replyCount) {
				topic.ReplyCount = replyCount;
			}

			var firstMessage = messages.First();

			topic.FirstMessageId = firstMessage.Id;
			topic.FirstMessageTimePosted = firstMessage.TimePosted;
			topic.FirstMessagePostedById = firstMessage.PostedById;
			topic.FirstMessageShortPreview = firstMessage.ShortPreview;

			var lastMessage = messages.Last();

			topic.LastMessageId = lastMessage.Id;
			topic.LastMessageTimePosted = lastMessage.TimePosted;
			topic.LastMessagePostedById = lastMessage.PostedById;
			topic.LastMessageShortPreview = lastMessage.ShortPreview;

			DbContext.Update(topic);
		}
	}
}
