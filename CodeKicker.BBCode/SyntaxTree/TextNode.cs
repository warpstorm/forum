using CodeKicker.BBCode.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CodeKicker.BBCode.SyntaxTree {
	public sealed class TextNode : SyntaxTreeNode {
		public string Text { get; }
		public string HtmlTemplate { get; }

		public TextNode(string text) : this(text, null) { }
		public TextNode(string text, string htmlTemplate) : base(null) {
			text.ThrowIfNull(nameof(text));
			Text = text;
			HtmlTemplate = htmlTemplate;
		}

		public override string ToHtml() => HtmlTemplate is null ? WebUtility.HtmlEncode(Text) : HtmlTemplate.Replace("${content}", WebUtility.HtmlEncode(Text));

		public override string ToBBCode() => Text.Replace("\\", "\\\\").Replace("[", "\\[").Replace("]", "\\]");

		public override string ToText() => Text;

		public override SyntaxTreeNode SetSubNodes(IList<SyntaxTreeNode> subNodes) {
			subNodes.ThrowIfNull(nameof(subNodes));

			if (subNodes.Any())
				throw new ArgumentException("subNodes cannot contain any nodes for a TextNode");

			return this;
		}

		internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor) {
			visitor.ThrowIfNull(nameof(visitor));
			return visitor.Visit(this);
		}

		protected override bool EqualsCore(SyntaxTreeNode b) {
			var casted = (TextNode) b;
			return Text == casted.Text && HtmlTemplate == casted.HtmlTemplate;
		}
	}
}