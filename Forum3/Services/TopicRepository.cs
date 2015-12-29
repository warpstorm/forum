using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Forum3.Data;
using Forum3.DataModels;
using Forum3.Helpers;
using Forum3.ViewModels.Topics;
using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;

namespace Forum3.Services {
	public class TopicRepository {
		private ApplicationDbContext _dbContext;
		private IHttpContextAccessor _httpContextAccessor;

		public TopicRepository(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor) {
			_dbContext = dbContext;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<TopicIndex> GetTopicIndexAsync(int skip, int take) {
			var messageRecords = _dbContext.Messages.Where(m => m.ParentId == 0).OrderByDescending(m => m.LastReplyPosted);

			var topicList = await messageRecords.Select(m => new TopicPartial {
				Id = m.Id,
				Subject = m.ShortPreview,
				LastReplyId = m.LastReplyId,
				LastReplyById = m.LastReplyById,
				LastReplyPostedDT = m.LastReplyPosted,
				Views = m.Views,
				Replies = m.Replies,
			})
				.ToListAsync();

			var skipped = 0;
			var topicIndex = new TopicIndex {
				Skip = skip + take,
				Take = take
			};

			foreach (var topic in topicList) {
				if (topicIndex.Topics.Count() > take) {
					topicIndex.MoreMessages = true;
					break;
				}

				if (skipped < skip) {
					skipped++;
					continue;
				}

				topicIndex.Topics.Add(topic);
			}

			return topicIndex;
		}

		public async Task<Topic> GetTopicAsync(int id, int currentPage, int skip, int take, bool jumpToLatest) {
			var topicFirstPost = await _dbContext.Messages.SingleOrDefaultAsync(m => m.Id == id);

			if (topicFirstPost == null)
				throw new Exception("No topic found with that ID.");

			if (topicFirstPost.ParentId != 0)
				throw new ChildMessageException(topicFirstPost.Id, topicFirstPost.ParentId);

			topicFirstPost.Views++;
			_dbContext.Entry(topicFirstPost).State = EntityState.Modified;
			var incrementViewsTask = _dbContext.SaveChangesAsync();

			var currentUser = _httpContextAccessor.HttpContext.User;
			var isAdmin = currentUser.Identity.IsAuthenticated && currentUser.IsInRole("Admin");

			var messageIds = await _dbContext.Messages.Where(m => m.Id == id || m.ParentId == id).Select(m => m.Id).ToListAsync();

			var totalMessages = messageIds.Count();
			var totalPages = take == 0 || totalMessages == 0 ? 1 : Convert.ToInt32(Math.Ceiling((double)totalMessages / take));

			var topic = new Topic {
				Id = topicFirstPost.Id,
				TopicHeader = new TopicHeader {
					StartedById = topicFirstPost.PostedById,
					Subject = topicFirstPost.ShortPreview,
					Views = topicFirstPost.Views,
				},
				Messages = new List<ViewModels.Messages.Message>(),
				//Boards = new List<IndexBoard>(),
				//AssignedBoards = new List<IndexBoard>(),
				CanManage = isAdmin || topicFirstPost.PostedById == currentUser.GetUserId(),
				CanInvite = isAdmin || topicFirstPost.PostedById == currentUser.GetUserId(),
				TotalPages = totalPages,
				CurrentPage = currentPage
			};

			var currentPageMessageIds = messageIds.Skip(skip).Take(take);

			var pageMessages = await _dbContext.Messages.Where(m => currentPageMessageIds.Contains(m.Id)).ToListAsync();
			var pagePosterIds = pageMessages.Select(m => m.PostedById).ToList();
			var pagePosters = _dbContext.Users.Where(u => pagePosterIds.Contains(u.Id));

			foreach (var messageRecord in pageMessages) {
				var postedBy = pagePosters.Single(u => u.Id == messageRecord.PostedById);

				Message repliedToMessage = null;
				ApplicationUser replyPostedBy = null;

				if (messageRecord.ReplyId != 0) {
					var getRepliedToMessageTask = _dbContext.Messages.SingleAsync(r => r.Id == messageRecord.ReplyId);
					var getReplyPostedByTask = _dbContext.Users.SingleAsync(r => r.Id == repliedToMessage.PostedById);

					repliedToMessage = await getRepliedToMessageTask;
					replyPostedBy = await getReplyPostedByTask;
				}

				topic.Messages.Add(new ViewModels.Messages.Message {
					Id = messageRecord.Id,
					ParentId = messageRecord.ParentId,
					ReplyId = messageRecord.ReplyId,
					ReplyBody = repliedToMessage?.DisplayBody,
					ReplyPreview = repliedToMessage?.ShortPreview,
					ReplyPostedBy = replyPostedBy?.DisplayName,
					Body = messageRecord.DisplayBody,
					PostedByName = postedBy.DisplayName,
					PostedById = messageRecord.PostedById,
					TimePosted = messageRecord.TimePosted.ToPassedTimeString(),
					TimeEdited = messageRecord.TimeEdited != messageRecord.TimePosted ? messageRecord.TimeEdited.ToPassedTimeString() : null,
					RecordTime = messageRecord.TimeEdited,
					CanEdit = isAdmin || (currentUser.Identity.IsAuthenticated && currentUser.GetUserId() == messageRecord.PostedById)
				});
			}

			await incrementViewsTask;

			return topic;
		}
	}
}
