using Narochno.BBCode;

namespace Forum.Services {
	public class BBCParserFactory {
		public static BBCodeParser GetParser() {
			return new BBCodeParser(new[] {
				new BBTag("b", @"<span class=""bbc-bold"">", "</span>"),
				new BBTag("s", @"<span class=""bbc-strike"">", "</span>"),
				new BBTag("i", @"<span class=""bbc-italic"">", "</span>"),
				new BBTag("u", @"<span class=""bbc-underline"">", "</span>"),
				new BBTag("ul", @"<ul class=""bbc-list"">", "</ul>"),
				new BBTag("ol", @"<ol class=""bbc-list"">", "</ol>"),
				ListItem(),
				Quote(),
				Spoiler(),
				Code(),
				Img(),
				Url(),
				Size(),
				Color(),
			});
		}

		static BBTag ListItem() {
			return new BBTag(
				name: "li",
				openTagTemplate: @"<li class=""bbc-list-item"">",
				closeTagTemplate: "</li>",
				autoRenderContent: true,
				requireClosingTag: true,
				contentTransformer: Trimmer
			);
		}

		static BBTag Spoiler() {
			return new BBTag(
				name: "spoiler",
				openTagTemplate: @"<span class=""bbc-spoiler"">",
				closeTagTemplate: "</span>",
				autoRenderContent: true,
				requireClosingTag: true,
				contentTransformer: Trimmer
			);
		}

		static BBTag Quote() {
			return new BBTag(
				name: "quote",
				openTagTemplate: @"<blockquote class=""bbc-quote"">",
				closeTagTemplate: "</blockquote>",
				autoRenderContent: true,
				requireClosingTag: true,
				contentTransformer: Trimmer
			);
		}

		static BBTag Code() {
			return new BBTag(
				name: "code",
				openTagTemplate: @"<div class=""bbc-code"">",
				closeTagTemplate: "</div>",
				autoRenderContent: true,
				requireClosingTag: true,
				contentTransformer: Trimmer
			);
		}

		static BBTag Img() {
			return new BBTag(
				name: "img",
				openTagTemplate: @"<img class=""bbc-image"" src=""${content}"" />",
				closeTagTemplate: "",
				autoRenderContent: false,
				requireClosingTag: true,
				contentTransformer: Trimmer
			);
		}

		static BBTag Url() {
			var attributes = new[] {
				new BBAttribute("href", ""),
				new BBAttribute("href", "href")
			};

			return new BBTag(
				name: "url",
				openTagTemplate: @"<a class=""bbc-anchor"" href=""${href}"" target=""_blank"">",
				closeTagTemplate: "</a>",
				attributes: attributes
			);
		}

		static BBTag Size() {
			var attributes = new[] {
				new BBAttribute("size", ""),
				new BBAttribute("size", "size")
			};

			return new BBTag(
				name: "size",
				openTagTemplate: @"<span style=""font-size: ${size}pt"">",
				closeTagTemplate: "</span>",
				attributes: attributes
			);
		}

		static BBTag Color() {
			var attributes = new[] {
				new BBAttribute("color", ""),
				new BBAttribute("color", "color")
			};

			return new BBTag(
				name: "color",
				openTagTemplate: @"<span style=""color: ${color};"">",
				closeTagTemplate: "</span>",
				attributes: attributes
			);
		}

		static System.Func<string, string> Trimmer = (contents) => {
			contents = contents.Trim();
			return contents;
		};
	}
}