using Forum3.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;

	public class RoleRepository {
		ApplicationDbContext DbContext { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }

		public RoleRepository(
			ApplicationDbContext dbContext,
			RoleManager<DataModels.ApplicationRole> roleManager
		) {
			DbContext = dbContext;
			RoleManager = roleManager;
		}

		public List<SelectListItem> PickList(int boardId) {
			var boardRolesQuery = from boardRole in DbContext.BoardRoles
								  where boardRole.BoardId == boardId
								  select boardRole.RoleId;

			var selectedItems = boardRolesQuery.ToList();

			var pickList = new List<SelectListItem>();

			var roles = RoleManager.Roles.OrderBy(r => r.Name).ToList();

			foreach (var role in roles) {
				pickList.Add(new SelectListItem {
					Text = role.Name,
					Value = role.Id,
					Selected = selectedItems.Contains(role.Id)
				});
			}

			return pickList;
		}
	}
}