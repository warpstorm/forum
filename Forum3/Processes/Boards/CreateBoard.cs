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

	public class CreateBoard {
		ApplicationDbContext DbContext { get; }
		IUrlHelper UrlHelper { get; }

		public CreateBoard(
			ApplicationDbContext dbContext,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public ServiceModels.ServiceResponse Execute(InputModels.CreateBoardInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (DbContext.Boards.Any(b => b.Name == input.Name))
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			DataModels.Category categoryRecord = null;

			if (!string.IsNullOrEmpty(input.NewCategory))
				input.NewCategory = input.NewCategory.Trim();

			if (!string.IsNullOrEmpty(input.NewCategory)) {
				categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Name == input.NewCategory);

				if (categoryRecord is null) {
					var displayOrder = DbContext.Categories.DefaultIfEmpty().Max(c => c.DisplayOrder);

					categoryRecord = new DataModels.Category {
						Name = input.NewCategory,
						DisplayOrder = displayOrder + 1
					};

					DbContext.Categories.Add(categoryRecord);
				}
			}
			else {
				try {
					var categoryId = Convert.ToInt32(input.Category);
					categoryRecord = DbContext.Categories.FirstOrDefault(c => c.Id == categoryId);

					if (categoryRecord is null)
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

			var existingRecord = DbContext.Boards.FirstOrDefault(b => b.Name == input.Name);

			if (existingRecord != null)
				serviceResponse.Error(nameof(input.Name), "A board with that name already exists");

			if (!serviceResponse.Success)
				return serviceResponse;

			DbContext.SaveChanges();

			var record = new DataModels.Board {
				Name = input.Name,
				Description = input.Description,
				CategoryId = categoryRecord.Id
			};

			DbContext.Boards.Add(record);

			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Controllers.Boards.Manage), nameof(Boards), new { id = record.Id });

			return serviceResponse;
		}
	}
}
