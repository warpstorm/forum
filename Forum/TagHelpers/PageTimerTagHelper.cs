using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Diagnostics;

namespace Forum.TagHelpers {
	public class PageTimerTagHelper : TagHelper {
		HttpContext HttpContext { get; }

		public PageTimerTagHelper(IHttpContextAccessor httpContextAccessor) => HttpContext = httpContextAccessor.HttpContext;

		public override void Process(TagHelperContext context, TagHelperOutput output) {
			var timerText = string.Empty;

			if (HttpContext.Items["PageTimer"] is Stopwatch pageTimer) {
				pageTimer.Stop();
				var pageTimerSeconds = 1D * pageTimer.ElapsedMilliseconds / 1000;
				timerText = $"Loaded in {pageTimerSeconds} seconds";
			}

			output.TagName = "p";
			output.TagMode = TagMode.StartTagAndEndTag;
			output.Attributes.SetAttribute("class", "font-tiny subdued-text");
			output.Content.SetContent(timerText);
		}
	}
}
