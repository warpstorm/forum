using System.Linq;

namespace Forum3.Processes.Boards {
	using ItemViewModels = Models.ViewModels.Boards.Items;
	using DataModels = Models.DataModels;
    using Forum3.Contexts;
    using Forum3.Extensions;

    public class LoadIndexBoard {
		ApplicationDbContext DbContext { get; }

		public LoadIndexBoard(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public ItemViewModels.IndexBoard Execute(DataModels.Board boardRecord) {
			var indexBoard = new ItemViewModels.IndexBoard {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				DisplayOrder = boardRecord.DisplayOrder,
				Unread = false
			};

			if (boardRecord.LastMessageId != null) {
				var lastMessageQuery = from lastReply in DbContext.Messages
									   where lastReply.Id == boardRecord.LastMessageId
									   join lastReplyBy in DbContext.Users on lastReply.LastReplyById equals lastReplyBy.Id
									   select new Models.ViewModels.Topics.Items.MessagePreview {
										   Id = lastReply.Id,
										   ShortPreview = lastReply.ShortPreview,
										   LastReplyByName = lastReplyBy.DisplayName,
										   LastReplyId = lastReply.LastReplyId,
										   LastReplyPosted = lastReply.LastReplyPosted.ToPassedTimeString(),
										   LastReplyPreview = lastReply.ShortPreview
									   };

				indexBoard.LastMessage = lastMessageQuery.FirstOrDefault();
			}

			return indexBoard;
		}
	}
}