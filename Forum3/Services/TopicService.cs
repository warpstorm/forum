using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Forum3.Data;
using Forum3.DataModels;
using Forum3.Helpers;
using PageModels = Forum3.ViewModels.Topics.Pages;
using ItemModels = Forum3.ViewModels.Topics.Items;

namespace Forum3.Services {
	public class TopicService {
		ApplicationDbContext DbContext { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		UserManager<ApplicationUser> UserManager { get; }

		public TopicService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager) {
			DbContext = dbContext;
			HttpContextAccessor = httpContextAccessor;
			UserManager = userManager;
		}

		public async Task<PageModels.TopicIndexPage> GetTopicIndex(int skip, int take) {
			var messageRecords = await (from m in DbContext.Messages
										where m.ParentId == 0
										orderby m.LastReplyPosted descending
										select new ViewModels.Topics.Items.MessagePreview {
											Id = m.Id,
											ShortPreview = m.ShortPreview,
											LastReplyId = m.LastReplyId == 0 ? m.Id : m.LastReplyId,
											LastReplyById = m.LastReplyById,
											LastReplyPostedDT = m.LastReplyPosted,
											Views = m.Views,
											Replies = m.Replies,
										})
									.Skip(skip).Take(take)
									.ToListAsync();

			return new PageModels.TopicIndexPage {
				Skip = skip + take,
				Take = take,
				Topics = messageRecords
			};
		}

		public async Task<PageModels.TopicDisplayPage> GetTopic(DataModels.Message parentMessage, int currentPage, int skip, int take, bool jumpToLatest) {
			parentMessage.Views++;
			DbContext.Entry(parentMessage).State = EntityState.Modified;
			DbContext.SaveChanges();

			var currentUser = await UserManager.GetUserAsync(HttpContextAccessor.HttpContext.User);
			var isAuthenticated = HttpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
			var isAdmin = isAuthenticated && HttpContextAccessor.HttpContext.User.IsInRole("Admin");

			// TEMP FIX BECAUSE EF7 LEFT OUTER JOINS ARE BROKEN. http://stackoverflow.com/a/34211463/2621693

			//var messages = await (
			//	from m in _dbContext.Messages
			//	join im in _dbContext.Messages on m.ReplyId equals im.Id into Replies
			//	from r in Replies.DefaultIfEmpty()
			//	where m.Id == id || m.ParentId == id
			//	select new ViewModels.Topics.Items.Message {
			//		Id = m.Id,
			//		ParentId = m.ParentId,
			//		ReplyId = m.ReplyId,
			//		ReplyBody = r == null ? string.Empty : r.DisplayBody,
			//		ReplyPreview = r == null ? string.Empty : r.LongPreview,
			//		ReplyPostedBy = r == null ? string.Empty : r.PostedByName,
			//		Body = m.DisplayBody,
			//		OriginalBody = m.OriginalBody,
			//		PostedByName = m.PostedByName,
			//		PostedById = m.PostedById,
			//		TimePostedDT = m.TimePosted,
			//		TimeEditedDT = m.TimeEdited,
			//		RecordTime = m.TimeEdited
			//	}
			//).Skip(skip).Take(take).ToListAsync();

			//foreach (var message in messages) {
			//	message.TimePosted = message.TimePostedDT.ToPassedTimeString();
			//	message.TimeEdited = message.TimeEditedDT.ToPassedTimeString();
			//	message.CanEdit = isAdmin || (isAuthenticated && currentUser.Id == message.PostedById);
			//	message.CanDelete = isAdmin || (isAuthenticated && currentUser.Id == message.PostedById);
			//	message.CanReply = isAuthenticated;
			//	message.CanThought = isAuthenticated;

			//	message.EditInput = new ViewModels.Topics.Items.EditPost {
			//		Id = message.Id,
			//		Body = message.OriginalBody,
			//	};

			//	message.ReplyInput = new ViewModels.Topics.Items.DirectReplyPost {
			//		Id = message.Id,
			//	};
			//}

			var messages = await(from m in DbContext.Messages
							 join im in DbContext.Messages on m.ReplyId equals im.Id into Replies
							 from r in Replies.DefaultIfEmpty()
							 where m.Id == parentMessage.Id || m.ParentId == parentMessage.Id
							 orderby m.Id
							 select new { m, r }).ToListAsync();

			var topic = new PageModels.TopicDisplayPage {
				Id = parentMessage.Id,
				TopicHeader = new ItemModels.TopicHeader {
					StartedById = parentMessage.PostedById,
					Subject = parentMessage.ShortPreview,
					Views = parentMessage.Views,
				},
				Messages = new List<ViewModels.Topics.Items.Message>(),
				//Boards = new List<IndexBoard>(),
				//AssignedBoards = new List<IndexBoard>(),
				IsAuthenticated = isAuthenticated,
				CanManage = isAdmin || parentMessage.PostedById == currentUser.Id,
				CanInvite = isAdmin || parentMessage.PostedById == currentUser.Id,
				TotalPages = take == 0 || messages.Count == 0 ? 1 : Convert.ToInt32(Math.Ceiling((double)messages.Count / take)),
				CurrentPage = currentPage,
				ReplyForm = new ItemModels.TopicReplyPost {
					Id = parentMessage.Id
				}
			};

			// TEMP FIX - REMOVE THIS LOOP AND UNCOMMENT BLOCK ABOVE WHEN EF7 LOJ ARE FIXED.
			// MAKE SURE YOU INCLUDE CHANGES TO THIS LOOP IN BLOCK ABOVE TOO!!

			foreach (var record in messages) {
				topic.Messages.Add(new ViewModels.Topics.Items.Message {
					Id = record.m.Id,
					ParentId = record.m.ParentId,
					ReplyId = record.m.ReplyId,
					ReplyBody = record.r?.DisplayBody,
					ReplyPreview = record.r?.LongPreview,
					ReplyPostedBy = record.r?.PostedByName,
					Body = record.m.DisplayBody,
					OriginalBody = record.m.OriginalBody,
					PostedByName = record.m.PostedByName,
					PostedById = record.m.PostedById,
					TimePostedDT = record.m.TimePosted,
					TimeEditedDT = record.m.TimeEdited,
					RecordTime = record.m.TimeEdited,
					TimePosted = record.m.TimePosted.ToPassedTimeString(),
					TimeEdited = record.m.TimeEdited.ToPassedTimeString(),
					CanEdit = isAdmin || (isAuthenticated && currentUser.Id == record.m.PostedById),
					CanDelete = isAdmin || (isAuthenticated && currentUser.Id == record.m.PostedById),
					CanReply = isAuthenticated,
					CanThought = isAuthenticated,
					EditForm = new ItemModels.EditPost {
						Id = record.m.Id,
						Body = record.m.OriginalBody
					},
					ReplyForm = new ItemModels.DirectReplyPost {
						Id = record.m.Id
					}
				});
			}

			return topic;
		}
	}
}
