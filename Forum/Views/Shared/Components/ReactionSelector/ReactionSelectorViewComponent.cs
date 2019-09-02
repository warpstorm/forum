using Microsoft.AspNetCore.Mvc;

namespace Forum.Views.Shared.Components.ReactionSelector {
	public class ReactionSelectorViewComponent : ViewComponent {
		public IViewComponentResult Invoke() {
			return View();
		}
	}
}
