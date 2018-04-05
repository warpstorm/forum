namespace Forum3.ViewModelProviders.Boards {
    using Forum3.Processes.Boards;
    using PageViewModels = Models.ViewModels.Boards.Pages;

	public class ManagePage {
		ListCategories ListCategories { get; }

		public ManagePage(
			ListCategories listCategories
		) {
			ListCategories = listCategories;
		}

		public PageViewModels.IndexPage Generate() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = ListCategories.Execute()
			};

			return viewModel;
		}
	}
}