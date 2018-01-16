using Forum3.Controllers;
using Forum3.Enums;
using Forum3.Helpers;
using Forum3.Models.InputModels;
using Forum3.Models.ViewModels.Boards.Items;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
	using ItemModels = Models.ViewModels.Topics.Items;
	using PageModels = Models.ViewModels.Topics.Pages;
	using ServiceModels = Models.ServiceModels;

	public class TopicService {
		DataModels.ApplicationDbContext DbContext { get; }
		BoardService BoardService { get; }
		SettingsRepository Settings { get; }
		ServiceModels.ContextUser ContextUser { get; }
		IUrlHelper UrlHelper { get; }

		public TopicService(
			DataModels.ApplicationDbContext dbContext,
			BoardService boardService,
			SettingsRepository settingsRepository,
			ContextUserFactory contextUserFactory,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			BoardService = boardService;
			Settings = settingsRepository;
			ContextUser = contextUserFactory.GetContextUser();
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<PageModels.TopicIndexPage> IndexPage(int boardId, int page) {
			var boardRecord = await DbContext.Boards.FindAsync(boardId);

			var messageIdQuery = from message in DbContext.Messages
								 orderby message.LastReplyPosted descending
								 join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
								 where boardRecord == null || messageBoard.BoardId == boardRecord.Id
								 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
								 from pin in pins.DefaultIfEmpty()
								 let pinned = pin != null && pin.UserId == ContextUser.ApplicationUser.Id
								 orderby (pinned ? pin.Id : 0) descending, message.LastReplyPosted descending
								 select message.Id;

			var messageIds = await messageIdQuery.ToListAsync();

			if (page < 1)
				page = 1;

			var take = Settings.TopicsPerPage();
			var skip = (page * take) - take;
			var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

			var pageMessageIds = messageIds.Skip(skip).Take(take).ToList();

			var topicPreviews = await GetTopicPreviews(pageMessageIds);

			return new PageModels.TopicIndexPage {
				BoardId = boardRecord?.Id ?? 0,
				BoardName = boardRecord?.Name ?? "All Topics",
				Skip = skip + take,
				Take = take,
				TotalPages = totalPages,
				CurrentPage = page,
				Topics = topicPreviews
			};
		}

		public async Task<PageModels.TopicDisplayPage> DisplayPage(int messageId, int page = 0, int target = 0) {
			var record = DbContext.Messages.Find(messageId);

			if (record is null)
				throw new Exception($"A record does not exist with ID '{messageId}'");

			var parentId = messageId;

			if (record.ParentId > 0)
				parentId = record.ParentId;

			var messageIdQuery = from message in DbContext.Messages
								 where message.Id == parentId || message.ParentId == parentId
								 select message.Id;

			var messageIds = messageIdQuery.ToList();

			if (parentId != messageId)
				return GetRedirectViewModel(messageId, record.ParentId, messageIds);

			if (!record.Processed)
				return GetMigrationRedirectViewModel(messageId);

			if (target > 0) {
				var targetPage = GetMessagePage(target, messageIds);

				if (targetPage != page)
					return GetRedirectViewModel(target, messageId, messageIds);
			}

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
				Categories = await BoardService.GetCategories(),
				AssignedBoards = new List<IndexBoard>(),
				IsAuthenticated = ContextUser.IsAuthenticated,
				CanManage = ContextUser.IsAdmin || record.PostedById == ContextUser.ApplicationUser.Id,
				TotalPages = totalPages,
				CurrentPage = page,
				ReplyForm = new ItemModels.ReplyForm {
					Id = record.Id
				}
			};

			var assignedBoardsQuery = from messageBoard in DbContext.MessageBoards
									  join board in DbContext.Boards on messageBoard.BoardId equals board.Id
									  where messageBoard.MessageId == topic.Id
									  select board;

			var assignedBoards = assignedBoardsQuery.ToList();

			foreach (var assignedBoard in assignedBoards) {
				var indexBoard = await BoardService.GetIndexBoard(assignedBoard);
				topic.AssignedBoards.Add(indexBoard);
			}

			await MarkTopicRead(topic);

			return topic;
		}

		public async Task<ServiceModels.ServiceResponse> Latest(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.Find(messageId);

			if (record is null) {
				serviceResponse.Error(string.Empty, $@"No record was found with the id '{messageId}'");
				return serviceResponse;
			}

			if (record.ParentId > 0)
				record = DbContext.Messages.Find(record.ParentId);

			if (!ContextUser.IsAuthenticated) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.LastReplyId });
				return serviceResponse;
			}

			var historyTimeLimit = Settings.HistoryTimeLimit();
			var viewLogs = await DbContext.ViewLogs.Where(r => r.UserId == ContextUser.ApplicationUser.Id && r.LogTime >= historyTimeLimit).ToListAsync();
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

			var latestMessageId = await messageIdQuery.FirstOrDefaultAsync();

			if (latestMessageId == 0)
				latestMessageId = record.LastReplyId;

			if (latestMessageId == 0)
				latestMessageId = record.Id;

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = latestMessageId });

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Pin(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.Find(messageId);

			if (record is null) {
				serviceResponse.Error(string.Empty, $@"No record was found with the id '{messageId}'");
				return serviceResponse;
			}

			if (record.ParentId > 0)
				messageId = record.ParentId;

			var existingRecord = await DbContext.Pins.FirstOrDefaultAsync(p => p.MessageId == messageId && p.UserId == ContextUser.ApplicationUser.Id);

			if (existingRecord is null) {
				var pinRecord = new DataModels.Pin {
					MessageId = messageId,
					Time = DateTime.Now,
					UserId = ContextUser.ApplicationUser.Id
				};

				DbContext.Pins.Add(pinRecord);
			}
			else
				DbContext.Pins.Remove(existingRecord);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> ToggleBoard(ToggleBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = DbContext.Messages.Find(input.MessageId);

			if (messageRecord is null)
				serviceResponse.Error(string.Empty, $@"No message was found with the id '{input.MessageId}'");

			var messageId = input.MessageId;

			if (messageRecord.ParentId > 0)
				messageId = messageRecord.ParentId;

			if (!await DbContext.Boards.AnyAsync(r => r.Id == input.BoardId))
				serviceResponse.Error(string.Empty, $@"No board was found with the id '{input.BoardId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var boardId = input.BoardId;

			var existingRecord = await DbContext.MessageBoards.FirstOrDefaultAsync(p => p.MessageId == messageId && p.BoardId == boardId);

			if (existingRecord is null) {
				var messageBoardRecord = new DataModels.MessageBoard {
					MessageId = messageId,
					BoardId = boardId,
					UserId = ContextUser.ApplicationUser.Id
				};

				DbContext.MessageBoards.Add(messageBoardRecord);
			}
			else
				DbContext.MessageBoards.Remove(existingRecord);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		async Task<List<ItemModels.MessagePreview>> GetTopicPreviews(List<int> messageIds) {
			var messageRecordQuery = from message in DbContext.Messages
									 where message.ParentId == 0 && messageIds.Contains(message.Id)
									 join reply in DbContext.Messages on message.LastReplyId equals reply.Id into replies
									 from reply in replies.DefaultIfEmpty()
									 join replyPostedBy in DbContext.Users on message.LastReplyById equals replyPostedBy.Id
									 join pin in DbContext.Pins on message.Id equals pin.MessageId into pins
									 from pin in pins.DefaultIfEmpty()
									 let pinned = pin != null && pin.UserId == ContextUser.ApplicationUser.Id
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

			var messages = await messageRecordQuery.ToListAsync();

			var participation = new List<DataModels.Participant>();
			var viewLogs = new List<DataModels.ViewLog>();
			var take = Settings.MessagesPerPage();

			if (ContextUser.IsAuthenticated) {
				participation = DbContext.Participants.Where(r => r.UserId == ContextUser.ApplicationUser.Id).ToList();

				var historyTimeLimit = Settings.HistoryTimeLimit();
				viewLogs = DbContext.ViewLogs.Where(r => r.UserId == ContextUser.ApplicationUser.Id && r.LogTime >= historyTimeLimit).ToList();
			}
			
			foreach (var message in messages) {
				message.Pages = Convert.ToInt32(Math.Ceiling(1.0 * message.Replies / take));
				message.LastReplyPosted = message.LastReplyPostedDT.ToPassedTimeString();

				var unread = 1;

				if (ContextUser.IsAuthenticated) {
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

			return messages;
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

				message.CanEdit = ContextUser.IsAdmin || (ContextUser.IsAuthenticated && ContextUser.ApplicationUser.Id == message.PostedById);
				message.CanDelete = ContextUser.IsAdmin || (ContextUser.IsAuthenticated && ContextUser.ApplicationUser.Id == message.PostedById);
				message.CanReply = ContextUser.IsAuthenticated;
				message.CanThought = ContextUser.IsAuthenticated;

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

		async Task MarkTopicRead(PageModels.TopicDisplayPage topic) {
			var historyTimeLimit = Settings.HistoryTimeLimit();

			var viewLogs = await DbContext.ViewLogs.Where(v =>
				v.LogTime >= historyTimeLimit
				&& v.UserId == ContextUser.ApplicationUser.Id
				&& (v.TargetType == EViewLogTargetType.All || (v.TargetType == EViewLogTargetType.Message && v.TargetId == topic.Id))
			).ToListAsync();

			DateTime latestTime;

			var latestMessageTime = topic.Messages.Max(r => r.RecordTime);

			if (viewLogs.Any()) {
				var latestViewLogTime = viewLogs.Max(r => r.LogTime);
				latestTime = latestViewLogTime > latestMessageTime ? latestViewLogTime : latestMessageTime;
			}
			else
				latestTime = latestMessageTime;

			foreach (var viewLog in await DbContext.ViewLogs.Where(r => r.UserId == ContextUser.ApplicationUser.Id && r.TargetId == topic.Id && r.TargetType == EViewLogTargetType.Message).ToListAsync())
				DbContext.ViewLogs.Remove(viewLog);

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				LogTime = latestTime,
				TargetId = topic.Id,
				TargetType = EViewLogTargetType.Message,
				UserId = ContextUser.ApplicationUser.Id
			});

			try {
				DbContext.SaveChanges();
			}
			// The user probably refreshed several times in a row.
			catch (DbUpdateConcurrencyException) { }
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
	}
}