using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Forum3.Data;
using Forum3.Helpers;
using Forum3.ViewModels.Messages;
using Forum3.ViewModels.Topics;
using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;

namespace Forum3.Services {
	public class TopicService {
		private ApplicationDbContext _dbContext;
		private IHttpContextAccessor _httpContextAccessor;

		public TopicService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor) {
			_dbContext = dbContext;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<TopicIndex> ConstructTopicIndexAsync(int skip, int take) {
			var messageRecords = _dbContext.Messages.Where(m => m.ParentId == 0).OrderByDescending(m => m.LastReplyPosted);

			var topicList = await messageRecords.Select(m => new TopicPartial {
				Id = m.Id,
				Subject = m.ShortPreview,
				LastReplyId = m.LastReplyId,
				LastReplyById = m.LastReplyById,
				LastReplyPostedDT = m.LastReplyPosted,
				Views = m.Views,
				Replies = m.Replies,
			}).ToListAsync();

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

		public async Task<Topic> ConstructTopicAsync(int id, int page, int skip, int take, bool jumpToLatest) {
			var topicFirstPost = await _dbContext.Messages.SingleOrDefaultAsync(m => m.Id == id);

			if (topicFirstPost == null)
				throw new Exception("No topic found with that ID.");

			if (topicFirstPost.ParentId != 0)
				throw new ChildMessageException(topicFirstPost.Id, topicFirstPost.ParentId);

			topicFirstPost.Views++;
			_dbContext.Entry(topicFirstPost).State = EntityState.Modified;
			var saveChangesTask = _dbContext.SaveChangesAsync();

			var currentUser = _httpContextAccessor.HttpContext.User;
			var isAdmin = currentUser.IsInRole("Admin");

			var topic = new Topic {
				TopicId = topicFirstPost.Id,
				StartedById = topicFirstPost.PostedById,
				Subject = topicFirstPost.ShortPreview,
				Messages = new List<Message>(),
				//Boards = new List<IndexBoard>(),
				//AssignedBoards = new List<IndexBoard>(),
				Views = topicFirstPost.Views,
				CanManage = isAdmin || topicFirstPost.PostedById == currentUser.GetUserId(),
				CanInvite = isAdmin || topicFirstPost.PostedById == currentUser.GetUserId()
			};

			await saveChangesTask;

			return topic;
		}
	}
}
