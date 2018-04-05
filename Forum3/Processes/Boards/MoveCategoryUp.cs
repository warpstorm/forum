using Forum3.Contexts;
using System.Linq;

namespace Forum3.Processes.Boards {
	using ServiceModels = Models.ServiceModels;

	public class MoveCategoryUp {
		ApplicationDbContext DbContext { get; }

		public MoveCategoryUp(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public ServiceModels.ServiceResponse Execute(int id) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var targetCategory = DbContext.Categories.FirstOrDefault(b => b.Id == id);

			if (targetCategory is null) {
				serviceResponse.Error(string.Empty, "No category found with that ID.");
				return serviceResponse;
			}

			if (targetCategory.DisplayOrder > 1) {
				var displacedCategory = DbContext.Categories.First(b => b.DisplayOrder == targetCategory.DisplayOrder - 1);

				displacedCategory.DisplayOrder++;
				DbContext.Update(displacedCategory);

				targetCategory.DisplayOrder--;
				DbContext.Update(targetCategory);

				DbContext.SaveChanges();
			}

			return serviceResponse;
		}
	}
}