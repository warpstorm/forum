using Forum3.Contexts;
using System.Linq;

namespace Forum3.Processes.Boards {
	using ServiceModels = Models.ServiceModels;

	public class MoveBoardUp {
		ApplicationDbContext DbContext { get; }

		public MoveBoardUp(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public ServiceModels.ServiceResponse Execute(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetBoard = DbContext.Boards.FirstOrDefault(b => b.Id == id);

			if (targetBoard is null) {
				serviceResponse.Error(string.Empty, "No board found with that ID.");
				return serviceResponse;
			}

			var categoryBoards = DbContext.Boards.Where(b => b.CategoryId == targetBoard.CategoryId).OrderBy(b => b.DisplayOrder).ToList();

			var currentIndex = 1;

			foreach (var board in categoryBoards) {
				board.DisplayOrder = currentIndex++;
				DbContext.Update(board);
			}

			DbContext.SaveChanges();

			targetBoard = categoryBoards.First(b => b.Id == id);

			if (targetBoard.DisplayOrder > 1) {
				var displacedBoard = categoryBoards.FirstOrDefault(b => b.DisplayOrder == targetBoard.DisplayOrder - 1);

				if (displacedBoard != null) {
					displacedBoard.DisplayOrder++;
					DbContext.Update(displacedBoard);
				}

				targetBoard.DisplayOrder--;
				DbContext.Update(targetBoard);

				DbContext.SaveChanges();
			}
			else
				targetBoard.DisplayOrder = 2;

			return serviceResponse;
		}
	}
}