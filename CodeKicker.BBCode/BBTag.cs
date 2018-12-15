using CodeKicker.BBCode.Helpers;
using System;

namespace CodeKicker.BBCode {
	public class BBTag {
		public const string ContentPlaceholderName = "content";

		public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, EBBTagClosingStyle tagClosingClosingStyle, Func<string, string> contentTransformer, bool enableIterationElementBehavior, params BBAttribute[] attributes) {
			name.ThrowIfNull(nameof(name));
			openTagTemplate.ThrowIfNull(nameof(openTagTemplate));
			closeTagTemplate.ThrowIfNull(nameof(closeTagTemplate), true);
			
			if (!Enum.IsDefined(typeof(EBBTagClosingStyle), tagClosingClosingStyle)) {
				throw new ArgumentException(nameof(tagClosingClosingStyle));
			}

			Name = name;
			OpenTagTemplate = openTagTemplate;
			CloseTagTemplate = closeTagTemplate ?? string.Empty;
			AutoRenderContent = autoRenderContent;
			TagClosingStyle = tagClosingClosingStyle;
			ContentTransformer = contentTransformer;
			EnableIterationElementBehavior = enableIterationElementBehavior;
			Attributes = attributes ?? new BBAttribute[0];
		}

		public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, EBBTagClosingStyle tagClosingClosingStyle, Func<string, string> contentTransformer, params BBAttribute[] attributes)
			: this(name, openTagTemplate, closeTagTemplate, autoRenderContent, tagClosingClosingStyle, contentTransformer, false, attributes) {
		}

		public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, bool requireClosingTag, Func<string, string> contentTransformer, params BBAttribute[] attributes)
			: this(name, openTagTemplate, closeTagTemplate, autoRenderContent, requireClosingTag ? EBBTagClosingStyle.RequiresClosingTag : EBBTagClosingStyle.AutoCloseElement, contentTransformer, attributes) {
		}

		public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, bool requireClosingTag, params BBAttribute[] attributes)
			: this(name, openTagTemplate, closeTagTemplate, autoRenderContent, requireClosingTag, null, attributes) {
		}

		public BBTag(string name, string openTagTemplate, string closeTagTemplate, params BBAttribute[] attributes)
			: this(name, openTagTemplate, closeTagTemplate, true, true, attributes) {
		}

		public string Name { get; }
		public string OpenTagTemplate { get; }
		public string CloseTagTemplate { get; }
		public bool AutoRenderContent { get; }
		public bool EnableIterationElementBehavior { get; }
		public bool RequiresClosingTag => TagClosingStyle == EBBTagClosingStyle.RequiresClosingTag;
		public EBBTagClosingStyle TagClosingStyle { get; }
		public Func<string, string> ContentTransformer { get; } //allows for custom modification of the tag content before rendering takes place
		public BBAttribute[] Attributes { get; }

		public BBAttribute FindAttribute(string name) => Array.Find(Attributes, a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}
}