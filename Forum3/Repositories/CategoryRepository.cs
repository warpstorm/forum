using Forum3.Contexts;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Repositories {
	using InputModels = Models.InputModels;
	using ItemViewModels = Models.ViewModels.Boards.Items;
	using ServiceModels = Models.ServiceModels;

	public class CategoryRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		BoardRepository BoardRepository { get; }
		RoleRepository RoleRepository { get; }

		public CategoryRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			RoleRepository roleRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			BoardRepository = boardRepository;
			RoleRepository = roleRepository;
		}

		public List<ItemViewModels.IndexCategory> Index() {
			var categories = DbContext.Categories.OrderBy(r => r.DisplayOrder).ToList();

			var indexCategories = new List<ItemViewModels.IndexCategory>();

			foreach (var categoryRecord in categories) {
				var indexCategory = new ItemViewModels.IndexCategory {
					Id = categoryRecord.Id,
					Name = categoryRecord.Name,
					DisplayOrder = categoryRecord.DisplayOrder
				};

				foreach (var board in BoardRepository.Where(r => r.CategoryId == categoryRecord.Id)) {
					var thisBoardRoles = RoleRepository.BoardRoles.Where(r => r.BoardId == board.Id);

					var authorized = UserContext.IsAdmin || !thisBoardRoles.Any() || (UserContext.Roles?.Any(userRole => thisBoardRoles.Any(boardRole => boardRole.RoleId == userRole)) ?? false);

					if (!authorized)
						continue;

					var indexBoard = BoardRepository.GetIndexItem(board);

					indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any())
					indexCategories.Add(indexCategory);
			}

			return indexCategories;
		}

		public List<SelectListItem> PickList() {
			var pickList = new List<SelectListItem>();

			var categoryRecords = DbContext.Categories.OrderBy(r => r.DisplayOrder).ToList();

			foreach (var categoryRecord in categoryRecords) {
				pickList.Add(new SelectListItem {
					Text = categoryRecord.Name,
					Value = categoryRecord.Id.ToString()
				});
			}

			return pickList;
		}

		public ServiceModels.ServiceResponse Merge(InputModels.MergeInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var fromCategory = DbContext.Categories.FirstOrDefault(b => b.Id == input.FromId);
			var toCategory = DbContext.Categories.FirstOrDefault(b => b.Id == input.ToId);

			if (fromCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.FromId}'");

			if (toCategory is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.ToId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var displacedBoards = BoardRepository.Where(b => b.CategoryId == fromCategory.Id).ToList();

			foreach (var displacedBoard in displacedBoards) {
				displacedBoard.CategoryId = toCategory.Id;
				DbContext.Update(displacedBoard);
			}

			DbContext.SaveChanges();

			DbContext.Categories.Remove(fromCategory);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public ServiceModels.ServiceResponse MoveUp(int id) {
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