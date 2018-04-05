using Forum3.Processes.Boards;
using System.Linq;

namespace Forum3.ViewModelProviders.Boards {
	using InputModels = Models.InputModels;
	using PageViewModels = Models.ViewModels.Boards.Pages;

	public class CreatePage {
		LoadCategoryPickList LoadCategoryPickList { get; }

		public CreatePage(
			LoadCategoryPickList loadCategoryPickList
		) {
			LoadCategoryPickList = loadCategoryPickList;
		}

		public PageViewModels.CreatePage Generate(InputModels.CreateBoardInput input = null) {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = LoadCategoryPickList.Execute()
			};

			if (input != null) {
				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
			}

			return viewModel;
		}
	}
}