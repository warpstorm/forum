using Forum3.Contexts;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Processes.Boards {
	public class LoadCategoryPickList {
		ApplicationDbContext DbContext { get; }

		public LoadCategoryPickList(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public List<SelectListItem> Execute() {
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
	}
}