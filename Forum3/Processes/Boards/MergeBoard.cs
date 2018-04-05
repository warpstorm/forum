using Forum3.Contexts;
using System.Linq;

namespace Forum3.Processes.Boards {
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;

	public class MergeBoard {
		ApplicationDbContext DbContext { get; }

		public MergeBoard(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public ServiceModels.ServiceResponse Execute(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromBoard = DbContext.Boards.FirstOrDefault(b => b.Id == input.FromId);
			var toBoard = DbContext.Boards.FirstOrDefault(b => b.Id == input.ToId);

			if (fromBoard is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toBoard is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var messageBoards = DbContext.MessageBoards.Where(m => m.BoardId == fromBoard.Id).ToList();

			// Reassign messages to new board
			foreach (var messageBoard in messageBoards) {
				messageBoard.BoardId = toBoard.Id;
				DbContext.Update(messageBoard);
			}

			DbContext.SaveChanges();

			var categoryId = fromBoard.CategoryId;

			// Delete the board
			DbContext.Boards.Remove(fromBoard);

			DbContext.SaveChanges();

			// Remove the category if empty
			if (!DbContext.Boards.Any(b => b.CategoryId == categoryId)) {
				var categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == categoryId);

				DbContext.Categories.Remove(categoryRecord);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}
	}
}