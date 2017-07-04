using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Forum3.Controllers;
using Forum3.Data;
using Forum3.Enums;
using Forum3.Helpers;
using Forum3.Models.DataModels;
using Forum3.Models.ServiceModels;
using Forum3.Models.ViewModels.Boards.Items;
using ItemModels = Forum3.Models.ViewModels.Topics.Items;
using PageModels = Forum3.Models.ViewModels.Topics.Pages;

namespace Forum3.Services {
	public class TopicService {
		ApplicationDbContext DbContext { get; }
		BoardService BoardService { get; }
		ContextUser ContextUser { get; }
		IUrlHelper UrlHelper { get; }

		public TopicService(
			ApplicationDbContext dbContext,
			BoardService boardService,
			ContextUserFactory contextUserFactory,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			BoardService = boardService;
			ContextUser = contextUserFactory.GetContextUser();
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<PageModels.TopicIndexPage> IndexPage(int boardId, int page) {
			var take = Constants.Defaults.MessagesPerPage;
			var skip = (page * take) - take;

			var boardRecord = await DbContext.Boards.FindAsync(boardId);

			var messageRecordQuery = from message in DbContext.Messages
									 join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
									 where boardRecord == null || messageBoard.BoardId == boardRecord.Id
									 where message.ParentId == 0
									 orderby message.LastReplyPosted descending
									 select new ItemModels.MessagePreview {
										 Id = message.Id,
										 ShortPreview = message.ShortPreview,
										 LastReplyId = message.LastReplyId == 0 ? message.Id : message.LastReplyId,
										 LastReplyById = message.LastReplyById,
										 LastReplyByName = message.LastReplyByName,
										 LastReplyPostedDT = message.LastReplyPosted,
										 Views = message.ViewCount,
										 Replies = message.ReplyCount,
									 };

			var messageRecords = await messageRecordQuery.Skip(skip).Take(take).ToListAsync();

			foreach (var message in messageRecords) {
				message.LastReplyPosted = message.LastReplyPostedDT.ToPassedTimeString();
			}

			return new PageModels.TopicIndexPage {
				BoardId = boardRecord?.Id ?? 0,
				BoardName = boardRecord?.Name ?? "All Topics",
				Skip = skip + take,
				Take = take,
				Topics = messageRecords
			};
		}

		public async Task<PageModels.TopicDisplayPage> DisplayPage(int messageId, int page = 0, int target = 0) {
			var record = await DbContext.Messages.FindAsync(messageId);

			if (record == null)
				throw new Exception($"A record does not exist with ID '{messageId}'");

			var parentId = messageId;

			if (record.ParentId > 0)
				parentId = record.ParentId;

			var messageIds = await DbContext.Messages.Where(m => m.Id == parentId || m.ParentId == parentId).Select(m => m.Id).ToListAsync();

			if (parentId != messageId)
				return GetRedirectViewModel(messageId, record.ParentId, messageIds);

			if (target > 0) {
				var targetPage = GetMessagePage(target, messageIds);

				if (targetPage != page)
					return GetRedirectViewModel(messageId, record.ParentId, messageIds);
			}

			if (page < 1)
				page = 1;

			var take = Constants.Defaults.MessagesPerPage;
			var skip = take * (page - 1);
			var totalPages = Convert.ToInt32(Math.Ceiling(1.0 * messageIds.Count / take));

			var pageMessageIds = messageIds.Skip(skip).Take(take);

			record.ViewCount++;
			DbContext.Entry(record).State = EntityState.Modified;
			DbContext.SaveChanges();

			var messages = await GetTopicMessages(pageMessageIds);

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

			var assignedBoards = await (
				from messageBoard in DbContext.MessageBoards
				join board in DbContext.Boards on messageBoard.BoardId equals board.Id
				where messageBoard.MessageId == topic.Id
				select board
			).ToListAsync();

			foreach (var assignedBoard in assignedBoards)
				topic.AssignedBoards.Add(BoardService.GetIndexBoard(assignedBoard));

			await MarkTopicRead(topic);

			return topic;
		}

		async Task<List<ItemModels.Message>> GetTopicMessages(IEnumerable<int> pageMessageIds) {
			var messageQuery = from m in DbContext.Messages
							   join im in DbContext.Messages on m.ReplyId equals im.Id into Replies
							   from r in Replies.DefaultIfEmpty()
							   where pageMessageIds.Contains(m.Id)
							   orderby m.Id
							   select new ItemModels.Message {
								   Id = m.Id,
								   ParentId = m.ParentId,
								   ReplyId = m.ReplyId,
								   ReplyBody = r == null ? string.Empty : r.DisplayBody,
								   ReplyPreview = r == null ? string.Empty : r.LongPreview,
								   ReplyPostedBy = r == null ? string.Empty : r.PostedByName,
								   Body = m.DisplayBody,
								   Cards = m.Cards,
								   OriginalBody = m.OriginalBody,
								   PostedByName = m.PostedByName,
								   PostedById = m.PostedById,
								   TimePostedDT = m.TimePosted,
								   TimeEditedDT = m.TimeEdited,
								   RecordTime = m.TimeEdited
							   };

			var messages = await messageQuery.ToListAsync();

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
			}

			return messages;
		}

		async Task MarkTopicRead(PageModels.TopicDisplayPage topic) {
			var historyTimeLimit = DateTime.Now.AddDays(Constants.Defaults.HistoryTimeLimit);

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

			foreach (var viewLog in DbContext.ViewLogs.Where(r => r.UserId == ContextUser.ApplicationUser.Id && r.TargetId == topic.Id && r.TargetType == EViewLogTargetType.Message).ToList())
				DbContext.ViewLogs.Remove(viewLog);

			await DbContext.SaveChangesAsync();

			await DbContext.ViewLogs.AddAsync(new ViewLog {
				LogTime = latestTime,
				TargetId = topic.Id,
				TargetType = EViewLogTargetType.Message,
				UserId = ContextUser.ApplicationUser.Id
			});
		}

		PageModels.TopicDisplayPage GetRedirectViewModel(int messageId, int parentMessageId, List<int> messageIds) {
			var viewModel = new PageModels.TopicDisplayPage();

			var routeValues = new {
				id = parentMessageId,
				page = GetMessagePage(messageId, messageIds),
				target = messageId
			};

			viewModel.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;

			return viewModel;
		}

		int GetMessagePage(int messageId, List<int> messageIds) {
			var index = (double) messageIds.FindIndex(id => id == messageId);
			return Convert.ToInt32(Math.Ceiling(index / Constants.Defaults.MessagesPerPage));
		}
	}
}