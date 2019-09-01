using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forum.Views.Shared.Components.SmileySelector {
	using ViewModels = Models.ViewModels.Smileys;

	public class SmileySelectorViewComponent : ViewComponent {
		SmileyRepository SmileyRepository { get; }

		public SmileySelectorViewComponent(SmileyRepository smileyRepository) => SmileyRepository = smileyRepository;

		public async Task<IViewComponentResult> InvokeAsync() {
			var items = new List<List<ViewModels.IndexItem>>();

			var currentColumn = -1;

			List<ViewModels.IndexItem> currentColumnList = null;

			foreach (var smiley in await SmileyRepository.Records()) {
				var sortColumn = smiley.SortOrder / 1000;
				var sortRow = smiley.SortOrder % 1000;

				if (currentColumn != sortColumn) {
					currentColumn = sortColumn;
					currentColumnList = new List<ViewModels.IndexItem>();
					items.Add(currentColumnList);
				}

				currentColumnList.Add(new ViewModels.IndexItem {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Thought = smiley.Thought,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return View("Default", items);
		}
	}
}
