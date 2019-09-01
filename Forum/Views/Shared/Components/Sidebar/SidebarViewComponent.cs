using Microsoft.AspNetCore.Mvc;

namespace Forum.Views.Shared.Components.Sidebar {
	public class SidebarViewComponent : ViewComponent {
		public IViewComponentResult Invoke() {
			return View();
		}
	}
}
