using Forum3.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Linq;

namespace Forum3.Processes.Boards {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;

	public class EditBoard {
		ApplicationDbContext DbContext { get; }
		IUrlHelper UrlHelper { get; }

		public EditBoard(
			ApplicationDbContext dbContext,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public ServiceModels.ServiceResponse Execute(InputModels.EditBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Boards.FirstOrDefault(b => b.Id == input.Id);

			if (record is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.Id}'");

			DataModels.Category newCategoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				newCategoryRecord = DbContext.Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (newCategoryRecord is null) {
					var displayOrder = DbContext.Categories.Max(c => c.DisplayOrder);

					newCategoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					DbContext.Categories.Add(newCategoryRecord);
					DbContext.SaveChanges();
				}
			}
			else {
				try {
					var newCategoryId = Convert.ToInt32(input.Category);
					newCategoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == newCategoryId);

					if (newCategoryRecord is null)
						serviceResponse.Error(nameof(input.Category), "No category was found with this ID.");
				}
				catch (FormatException) {
					serviceResponse.Error(nameof(input.Category), "Invalid category ID");
				}
			}

			if (!string.IsNullOrEmpty(input.Name))
				input.Name = input.Name.Trim();

			if (string.IsNullOrEmpty(input.Name))
				serviceResponse.Error(nameof(input.Name), "Name is a required field.");

			if (!string.IsNullOrEmpty(input.Description))
				input.Description = input.Description.Trim();

			if (!serviceResponse.Success)
				return serviceResponse;

			record.Name = input.Name;
			record.Description = input.Description;

			var oldCategoryId = -1;

			if (record.CategoryId != newCategoryRecord.Id) {
				var categoryBoards = DbContext.Boards.Where(r => r.CategoryId == record.CategoryId).ToList();

				if (categoryBoards.Count() <= 1)
					oldCategoryId = record.CategoryId;

				record.CategoryId = newCategoryRecord.Id;
			}

			var boardRoles = DbContext.BoardRoles.Where(r => r.BoardId == record.Id).ToList();

			foreach (var boardRole in boardRoles)
				DbContext.BoardRoles.Remove(boardRole);

			if (input.Roles != null) {
				var roleIds = DbContext.Roles.Select(r => r.Id).ToList();

				foreach (var inputRole in input.Roles) {
					if (roleIds.Contains(inputRole)) {
						DbContext.BoardRoles.Add(new DataModels.BoardRole {
							BoardId = record.Id,
							RoleId = inputRole
						});
					}
					else
						serviceResponse.Error($"Role does not exist with id '{inputRole}'");
				}
			}

			if (!serviceResponse.Success)
				return serviceResponse;

			DbContext.Update(record);
			DbContext.SaveChanges();

			if (oldCategoryId >= 0) {
				var oldCategoryRecord = DbContext.Categories.Find(oldCategoryId);
				DbContext.Categories.Remove(oldCategoryRecord);
				DbContext.SaveChanges();
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Boards.Manage), nameof(Boards), new { id = record.Id });

			return serviceResponse;
		}
	}
}
