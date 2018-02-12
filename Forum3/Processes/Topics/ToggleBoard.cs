using Forum3.Contexts;
using Forum3.Exceptions;
using System.Linq;

namespace Forum3.Processes.Topics {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;

	public class ToggleBoard {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }

		public ToggleBoard(
			ApplicationDbContext dbContext,
			UserContext userContext
		) {
			DbContext = dbContext;
			UserContext = userContext;
		}

		public ServiceModels.ServiceResponse Execute(InputModels.ToggleBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = DbContext.Messages.Find(input.MessageId);

			if (messageRecord is null)
				throw new HttpNotFoundException($@"No message was found with the id '{input.MessageId}'");

			var messageId = input.MessageId;

			if (messageRecord.ParentId > 0)
				messageId = messageRecord.ParentId;

			if (!DbContext.Boards.Any(r => r.Id == input.BoardId))
				serviceResponse.Error(string.Empty, $@"No board was found with the id '{input.BoardId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var boardId = input.BoardId;

			var existingRecord = DbContext.MessageBoards.FirstOrDefault(p => p.MessageId == messageId && p.BoardId == boardId);

			if (existingRecord is null) {
				var messageBoardRecord = new DataModels.MessageBoard {
					MessageId = messageId,
					BoardId = boardId,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.MessageBoards.Add(messageBoardRecord);
			}
			else
				DbContext.MessageBoards.Remove(existingRecord);

			DbContext.SaveChanges();

			return serviceResponse;
		}
	}
}