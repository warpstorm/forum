using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forum.TagHelpers {
	public class CancelButtonTagHelper : TagHelper {
		HttpContext HttpContext { get; }

		public CancelButtonTagHelper(
			IActionContextAccessor actionContextAccessor
		) {
			HttpContext = actionContextAccessor.ActionContext.HttpContext;
		}

		public override void Process(TagHelperContext context, TagHelperOutput output) {
			output.TagName = "a";

			if (output.Attributes.ContainsName("class")) {
				var currentClasses = output.Attributes.TryGetAttribute("class", out var classes);				
				var newClasses = $"{classes.Value} button cancel-button";
				output.Attributes.SetAttribute("class", newClasses);
			}
			else {
				output.Attributes.SetAttribute("class", "button cancel-button");
			}

			var returnUrl = GetReturnUrl();
			output.Attributes.SetAttribute("href", returnUrl);

			if (string.IsNullOrEmpty(output.Content.GetContent())) {
				output.Content.SetContent("Cancel");
			}

			output.TagMode = TagMode.StartTagAndEndTag;
		}

		string GetReturnUrl() {
			HttpContext.Request.Query.TryGetValue("ReturnUrl", out var returnUrl);

			if (string.IsNullOrEmpty(returnUrl)) {
				HttpContext.Request.Query.TryGetValue("Referer", out returnUrl);
			}

			if (string.IsNullOrEmpty(returnUrl)) {
				returnUrl = "/";
			}

			return returnUrl;
		}
	}
}