using Forum3.Contexts;
using Forum3.Processes.Boards;
using System;
using System.Linq;

namespace Forum3.ViewModelProviders.Boards {
	using InputModels = Models.InputModels;
	using PageViewModels = Models.ViewModels.Boards.Pages;

	public class EditPage {
		ApplicationDbContext DbContext { get; }
		LoadCategoryPickList LoadCategoryPickList { get; }
		LoadRolePickList LoadRolePickList { get; }

		public EditPage(
			ApplicationDbContext dbContext,
			LoadCategoryPickList loadCategoryPickList,
			LoadRolePickList loadRolePickList
		) {
			DbContext = dbContext;
			LoadCategoryPickList = loadCategoryPickList;
			LoadRolePickList = loadRolePickList;
		}

		public PageViewModels.EditPage Generate(int boardId, InputModels.EditBoardInput input = null) {
			var boardRecord = DbContext.Boards.FirstOrDefault(b => b.Id == boardId);

			if (boardRecord is null)
				throw new Exception($"A record does not exist with ID '{boardId}'");

			var viewModel = new PageViewModels.EditPage() {
				Id = boardRecord.Id,
				Categories = LoadCategoryPickList.Execute(),
				Roles = LoadRolePickList.Execute(boardRecord.Id)
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}
			else {
				var category = DbContext.Categories.Find(boardRecord.CategoryId);

				viewModel.Name = boardRecord.Name;
				viewModel.Description = boardRecord.Description;
				viewModel.Categories.First(item => item.Text == category.Name).Selected = true;
			}

			return viewModel;
		}
	}
}