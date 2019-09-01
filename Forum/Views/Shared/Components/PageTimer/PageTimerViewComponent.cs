using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Forum.Views.Shared.Components.PageTimer {
	public class PageTimerViewComponent : ViewComponent {
		public IViewComponentResult Invoke() {
			var timerText = string.Empty;

			if (false && HttpContext.Items["PageTimer"] is Stopwatch pageTimer) {
				pageTimer.Stop();
				var pageTimerSeconds = 1D * pageTimer.ElapsedMilliseconds / 1000;
				timerText = $"Loaded in {pageTimerSeconds} seconds";
			}

			return View("Default", timerText);
		}
	}
}
