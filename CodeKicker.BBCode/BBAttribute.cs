using CodeKicker.BBCode.Helpers;
using System;

namespace CodeKicker.BBCode {
	public class BBAttribute {
		public string ID { get; } //ID is used to reference the attribute value
		public string Name { get; } //Name is used during parsing
		public Func<IAttributeRenderingContext, string> ContentTransformer { get; } //allows for custom modification of the attribute value before rendering takes place
		public EHtmlEncodingMode HtmlEncodingMode { get; set; }

		public BBAttribute(string id, string name) : this(id, name, null, EHtmlEncodingMode.HtmlAttributeEncode) { }
		public BBAttribute(string id, string name, Func<IAttributeRenderingContext, string> contentTransformer) : this(id, name, contentTransformer, EHtmlEncodingMode.HtmlAttributeEncode) { }
		public BBAttribute(string id, string name, Func<IAttributeRenderingContext, string> contentTransformer, EHtmlEncodingMode htmlEncodingMode) {
			id.ThrowIfNull(nameof(id));
			name.ThrowIfNull(nameof(name), true);

			if (!Enum.IsDefined(typeof(EHtmlEncodingMode), htmlEncodingMode))
				throw new ArgumentException("htmlEncodingMode");

			ID = id;
			Name = name;
			ContentTransformer = contentTransformer;
			HtmlEncodingMode = htmlEncodingMode;
		}

		public static Func<IAttributeRenderingContext, string> AdaptLegacyContentTransformer(Func<string, string> contentTransformer) {
			return contentTransformer is null ? (Func<IAttributeRenderingContext, string>) null : ctx => contentTransformer(ctx.AttributeValue);
		}
	}
}