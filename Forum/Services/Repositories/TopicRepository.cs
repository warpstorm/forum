using Forum.Controllers;
using Forum.Models.Errors;
using Forum.Models.Options;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using ServiceModels = Models.ServiceModels;

	public class TopicRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		BookmarkRepository BookmarkRepository { get; }
		MessageRepository MessageRepository { get; }
		NotificationRepository NotificationRepository { get; }
		RoleRepository RoleRepository { get; }
		SmileyRepository SmileyRepository { get; }

		IUrlHelper UrlHelper { get; }

		public TopicRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			BookmarkRepository bookmarkRepository,
			MessageRepository messageRepository,
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
			BookmarkRepository = bookmarkRepository;
			MessageRepository = messageRepository;
			NotificationRepository = notificationRepository;
			RoleRepository = roleRepository;
			SmileyRepository = smileyRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<ServiceModels.ServiceResponse> GetFirstUnreadMessage(int topicId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

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

						case EViewLogTargetType.Message:
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

				latestMessageId = await messageIdQuery.FirstOrDefaultAsync();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = topicId, target = latestMessageId });

			return serviceResponse;
		}

		public async Task<List<ItemModels.TopicPreview>> GetPreviews(List<int> topicIds) {
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

			var topicPreviews = new List<ItemModels.TopicPreview>();
			var today = DateTime.Now.Date;

			var boards = await BoardRepository.Records();

			foreach (var topicId in topicIds) {
				var topic = topics.First(item => item.Id == topicId);
				var firstMessagePostedBy = users.First(r => r.Id == topic.FirstMessagePostedById);
				var lastMessagePostedBy = users.First(r => r.Id == topic.LastMessagePostedById);

				var topicPreview = new ItemModels.TopicPreview {
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
											  select new Models.ViewModels.Boards.Items.IndexBoard {
												  Id = board.Id.ToString(),
												  Name = board.Name,
												  Description = board.Description,
												  DisplayOrder = board.DisplayOrder,
											  };

				topicPreview.Boards = topicPreviewBoardsQuery.ToList();
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
							 join topicBoard in DbContext.TopicBoards on topic.Id equals topicBoard.MessageId
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

			var existingRecord = await DbContext.TopicBoards.FirstOrDefaultAsync(p => p.MessageId == input.TopicId && p.BoardId == input.BoardId);

			if (existingRecord is null) {
				var topicBoardRecord = new DataModels.TopicBoard {
					TopicId = input.TopicId,
					BoardId = input.BoardId,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.TopicBoards.Add(topicBoardRecord);
			}
			else {
				DbContext.TopicBoards.Remove(existingRecord);
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
			await RebuildTopic(targetTopic);
			await RemoveTopicViewLogs(targetTopic.Id);
			await RemoveTopicArtifacts(sourceTopic);
		}

		public async Task UpdateMessagesTopicId(DataModels.Topic sourceTopic, DataModels.Topic targetTopic) {
			var sourceMessages = await DbContext.Messages.Where(item => item.TopicId == sourceTopic.Id).ToListAsync();

			foreach (var message in sourceMessages) {
				message.TopicId = targetTopic.Id;
				DbContext.Update(message);
			}

			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicArtifacts(DataModels.Topic topic) {
			await RemoveTopicViewLogs(topic.Id);
			await RemoveTopicBookmarks(topic.Id);
			await RemoveTopicBoards(topic.Id);

			DbContext.Remove(topic);
			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicViewLogs(int topicId) {
			var records = await DbContext.ViewLogs.Where(item => item.TargetType == EViewLogTargetType.Topic && item.TargetId == topicId).ToListAsync();
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicBookmarks(int topicId) {
			var records = DbContext.Bookmarks.Where(item => item.MessageId == topicId);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RemoveTopicBoards(int topicId) {
			var records = DbContext.TopicBoards.Where(item => item.MessageId == topicId);
			DbContext.RemoveRange(records);
			await DbContext.SaveChangesAsync();
		}

		public async Task RebuildTopic(DataModels.Topic topic) {
			var messagesQuery = from message in DbContext.Messages
								where message.TopicId == topic.Id
								where !message.Deleted
								select message;

			var messages = await messagesQuery.ToListAsync();

			var replyCount = messages.Count();

			if (topic.ReplyCount != replyCount) {
				topic.ReplyCount = replyCount;
			}

			var firstMessage = messages.FirstOrDefault();

			topic.FirstMessageId = firstMessage.Id;
			topic.FirstMessageTimePosted = firstMessage.TimePosted;
			topic.FirstMessagePostedById = firstMessage.PostedById;
			topic.FirstMessageShortPreview = firstMessage.ShortPreview;

			var lastMessage = messages.LastOrDefault();

			topic.LastMessageId = lastMessage.Id;
			topic.LastMessageTimePosted = lastMessage.TimePosted;
			topic.LastMessagePostedById = lastMessage.PostedById;
			topic.LastMessageShortPreview = lastMessage.ShortPreview;

			DbContext.Update(topic);

			if (topic.Deleted) {
				foreach (var message in messages) {
					await MessageRepository.DeleteMessage(message.Id);
				}
			}

			await DbContext.SaveChangesAsync();

			await RebuildParticipantsForTopic(topic.Id);
		}

		public async Task RebuildParticipantsForTopic(int topicId) {
			var messagesQuery = from message in DbContext.Messages
								where message.TopicId == topicId
								select new {
									message.PostedById,
									message.TimePosted
								};

			var messages = await messagesQuery.ToListAsync();

			var newParticipants = new List<DataModels.Participant>();

			foreach (var message in messages) {
				if (!newParticipants.Any(item => item.UserId == message.PostedById)) {
					newParticipants.Add(new DataModels.Participant {
						TopicId = topicId,
						UserId = message.PostedById,
						Time = message.TimePosted
					});
				}
			}

			var oldParticipants = await DbContext.Participants.Where(r => r.TopicId == topicId).ToListAsync();

			DbContext.RemoveRange(oldParticipants);
			await DbContext.SaveChangesAsync();

			DbContext.Participants.AddRange(newParticipants);
			await DbContext.SaveChangesAsync();
		}
	}
}
