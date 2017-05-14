using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Forum3.Data;
using Forum3.Helpers;
using PageModels = Forum3.Models.ViewModels.Topics.Pages;
using ItemModels = Forum3.Models.ViewModels.Topics.Items;
using Microsoft.AspNetCore.Mvc;
using Forum3.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Forum3.Models.DataModels;
using Forum3.Enums;

namespace Forum3.Services {
	public class TopicService {
		ApplicationDbContext DbContext { get; }
		UserService UserService { get; set; }

		IUrlHelperFactory UrlHelperFactory { get; set; }
		IActionContextAccessor ActionContextAccessor { get; set; }

		public TopicService(
			ApplicationDbContext dbContext,
			UserService userService,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserService = userService;
			ActionContextAccessor = actionContextAccessor;
			UrlHelperFactory = urlHelperFactory;
		}

		public async Task<PageModels.TopicIndexPage> IndexPage(int page) {
			var take = Constants.Defaults.MessagesPerPage;
			var skip = (page * take) - take;

			var messageRecordQuery = from m in DbContext.Messages
									 where m.ParentId == 0
									 orderby m.LastReplyPosted descending
									 select new ItemModels.MessagePreview {
										 Id = m.Id,
										 ShortPreview = m.ShortPreview,
										 LastReplyId = m.LastReplyId == 0 ? m.Id : m.LastReplyId,
										 LastReplyById = m.LastReplyById,
										 LastReplyPostedDT = m.LastReplyPosted,
										 Views = m.Views,
										 Replies = m.Replies,
									 };

			var messageRecords = await messageRecordQuery.Skip(skip).Take(take).ToListAsync();

			return new PageModels.TopicIndexPage {
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

			if (page < 1)
				page = 1;

			var take = Constants.Defaults.MessagesPerPage;
			var skip = take * (page - 1);
			var totalPages = Convert.ToInt32(Math.Ceiling(messageIds.Count / take * 1.0));

			var pageMessageIds = messageIds.Skip(skip).Take(take);

			record.Views++;
			DbContext.Entry(record).State = EntityState.Modified;
			DbContext.SaveChanges();

			var currentUser = UserService.ContextUser;

			var messageQuery = from m in DbContext.Messages
							   join im in DbContext.Messages on m.ReplyId equals im.Id into Replies
							   from r in Replies.DefaultIfEmpty()
							   where pageMessageIds.Contains(m.Id)
							   select new ItemModels.Message {
								   Id = m.Id,
								   ParentId = m.ParentId,
								   ReplyId = m.ReplyId,
								   ReplyBody = r == null ? string.Empty : r.DisplayBody,
								   ReplyPreview = r == null ? string.Empty : r.LongPreview,
								   ReplyPostedBy = r == null ? string.Empty : r.PostedByName,
								   Body = m.DisplayBody,
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

				message.CanEdit = currentUser.IsAdmin || (currentUser.IsAuthenticated && currentUser.ApplicationUser.Id == message.PostedById);
				message.CanDelete = currentUser.IsAdmin || (currentUser.IsAuthenticated && currentUser.ApplicationUser.Id == message.PostedById);
				message.CanReply = currentUser.IsAuthenticated;
				message.CanThought = currentUser.IsAuthenticated;

				message.ReplyForm = new ItemModels.ReplyForm {
					Id = message.Id,
				};
			}
			
			var topic = new PageModels.TopicDisplayPage {
				Id = record.Id,
				TopicHeader = new ItemModels.TopicHeader {
					StartedById = record.PostedById,
					Subject = record.ShortPreview,
					Views = record.Views,
				},
				Messages = messages,
				//Boards = new List<IndexBoard>(),
				//AssignedBoards = new List<IndexBoard>(),
				IsAuthenticated = currentUser.IsAuthenticated,
				CanManage = currentUser.IsAdmin || record.PostedById == currentUser.ApplicationUser.Id,
				TotalPages = totalPages,
				CurrentPage = page,
				ReplyForm = new ItemModels.ReplyForm {
					Id = record.Id
				}
			};

			return topic;
		}

		PageModels.TopicDisplayPage GetRedirectViewModel(int messageId, int parentMessageId, List<int> messageIds) {
			var viewModel = new PageModels.TopicDisplayPage();

			var routeValues = new {
				id = parentMessageId,
				page = GetMessagePage(messageId, messageIds),
				target = messageId
			};

			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);

			viewModel.RedirectPath = urlHelper.Action(nameof(Topics.Display), nameof(Topics), routeValues) + "#message" + messageId;

			return viewModel;
		}

		int GetMessagePage(int messageId, List<int> messageIds) {
			var index = 0;
			while (messageIds[index] != messageId)
				index++;

			return Convert.ToInt32(Math.Ceiling(index / Constants.Defaults.MessagesPerPage * 1.0));
		}
	}
}