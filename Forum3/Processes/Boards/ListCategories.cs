using Forum3.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Processes.Boards {
	using ItemViewModels = Models.ViewModels.Boards.Items;

	public class ListCategories {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		LoadIndexBoard IndexBoard { get; }

		public ListCategories(
			ApplicationDbContext dbContext,
			UserContext userContext,
			LoadIndexBoard indexBoard
		) {
			DbContext = dbContext;
			UserContext = userContext;
			IndexBoard = indexBoard;
		}

		public List<ItemViewModels.IndexCategory> Execute() {
			var categoryRecordsTask = DbContext.Categories.OrderBy(r => r.DisplayOrder).ToListAsync();
			var boardRecordsTask = DbContext.Boards.OrderBy(r => r.DisplayOrder).ToListAsync();
			var boardRoleRecordsTask = DbContext.BoardRoles.ToListAsync();

			Task.WaitAll(categoryRecordsTask, boardRecordsTask, boardRoleRecordsTask);

			var categories = categoryRecordsTask.Result;
			var boards = boardRecordsTask.Result;
			var boardRoles = boardRoleRecordsTask.Result;

			var indexCategories = new List<ItemViewModels.IndexCategory>();

			foreach (var categoryRecord in categories) {
				var indexCategory = new ItemViewModels.IndexCategory {
					Id = categoryRecord.Id,
					Name = categoryRecord.Name,
					DisplayOrder = categoryRecord.DisplayOrder
				};

				foreach (var board in boards.Where(r => r.CategoryId == categoryRecord.Id)) {
					var thisBoardRoles = boardRoles.Where(r => r.BoardId == board.Id);

					var authorized = UserContext.IsAdmin || !thisBoardRoles.Any() || UserContext.Roles.Any(userRole => thisBoardRoles.Any(boardRole => boardRole.RoleId == userRole));

					if (!authorized)
						continue;

					var indexBoard = IndexBoard.Execute(board);

					indexCategory.Boards.Add(indexBoard);
				}

				// Don't index the category if there's no boards available to the user
				if (indexCategory.Boards.Any())
					indexCategories.Add(indexCategory);
			}

			return indexCategories;
		}
	}
}