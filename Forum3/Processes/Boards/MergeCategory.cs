using Forum3.Contexts;
using System.Linq;

namespace Forum3.Processes.Boards {
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;

	public class MergeCategory {
		ApplicationDbContext DbContext { get; }

		public MergeCategory(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public ServiceModels.ServiceResponse Execute(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromCategory = DbContext.Categories.FirstOrDefault(b => b.Id == input.FromId);
			var toCategory = DbContext.Categories.FirstOrDefault(b => b.Id == input.ToId);

			if (fromCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var displacedBoards = DbContext.Boards.Where(b => b.CategoryId == fromCategory.Id).ToList();

			foreach (var displacedBoard in displacedBoards) {
				displacedBoard.CategoryId = toCategory.Id;
				DbContext.Update(displacedBoard);
			}

			DbContext.SaveChanges();

			DbContext.Categories.Remove(fromCategory);

			DbContext.SaveChanges();

			return serviceResponse;
		}
	}
}