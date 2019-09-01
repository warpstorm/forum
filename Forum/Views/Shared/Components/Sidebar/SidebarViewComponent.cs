using Microsoft.AspNetCore.Mvc;

namespace Forum.Views.Shared.Components.Quote {
	public class SidebarViewComponent : ViewComponent {
		public IViewComponentResult Invoke() {
			return View();
		}
	}
}
