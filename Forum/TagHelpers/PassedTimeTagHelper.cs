using Forum.Extensions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace Forum.TagHelpers {
	public class PassedTimeTagHelper : TagHelper {
		public DateTime Time { get; set; }

		public override void Process(TagHelperContext context, TagHelperOutput output) {
			output.TagName = "time";
			output.TagMode = TagMode.StartTagAndEndTag;
			output.Attributes.SetAttribute("datetime", Time.ToHtmlLocalTimeString());

			if (string.IsNullOrEmpty(output.Content.GetContent())) {
				output.Content.SetContent(Time.ToPassedTimeString());
			}
		}
	}
}
