using Forum.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Forum.TagHelpers {
	public class CancelButtonTagHelper : TagHelper {
		HttpContext HttpContext { get; }
		IUrlHelper UrlHelper { get; }

		public CancelButtonTagHelper(
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			HttpContext = actionContextAccessor.ActionContext.HttpContext;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public override void Process(TagHelperContext context, TagHelperOutput output) {
			output.TagName = "a";

			if (!output.Attributes.ContainsName("class")) {
				output.Attributes.SetAttribute("class", "button");
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
				returnUrl = UrlHelper.Action(nameof(Home.FrontPage), nameof(Home));
			}

			return returnUrl;
		}
	}
}