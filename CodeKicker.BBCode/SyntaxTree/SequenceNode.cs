using CodeKicker.BBCode.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace CodeKicker.BBCode.SyntaxTree {
	public sealed class SequenceNode : SyntaxTreeNode {
		public SequenceNode() { }
		public SequenceNode(SyntaxTreeNodeCollection subNodes) : base(subNodes) { }
		public SequenceNode(IList<SyntaxTreeNode> subNodes) : base(subNodes) { }

		public override string ToHtml() => string.Concat(SubNodes.Select(s => s.ToHtml()).ToArray());

		public override string ToBBCode() => string.Concat(SubNodes.Select(s => s.ToBBCode()).ToArray());

		public override string ToText() => string.Concat(SubNodes.Select(s => s.ToText()).ToArray());

		public override SyntaxTreeNode SetSubNodes(IList<SyntaxTreeNode> subNodes) {
			subNodes.ThrowIfNull(nameof(subNodes));
			return new SequenceNode(subNodes);
		}

		internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor) {
			visitor.ThrowIfNull(nameof(visitor));
			return visitor.Visit(this);
		}

		protected override bool EqualsCore(SyntaxTreeNode b) => true;
	}
}