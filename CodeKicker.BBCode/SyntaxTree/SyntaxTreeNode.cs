using System;
using System.Collections.Generic;

namespace CodeKicker.BBCode.SyntaxTree {
	public abstract class SyntaxTreeNode : IEquatable<SyntaxTreeNode> {
		public ISyntaxTreeNodeCollection SubNodes { get; private set; } = new SyntaxTreeNodeCollection();

		protected SyntaxTreeNode() {}
		protected SyntaxTreeNode(ISyntaxTreeNodeCollection subNodes) {
			SubNodes = subNodes;
		}
		protected SyntaxTreeNode(IList<SyntaxTreeNode> subNodes) {
			SubNodes = new SyntaxTreeNodeCollection(subNodes);
		}

		public override string ToString() {
			return ToBBCode();
		}

		public abstract string ToHtml();
		public abstract string ToBBCode();
		public abstract string ToText();

		public abstract SyntaxTreeNode SetSubNodes(IList<SyntaxTreeNode> subNodes);
		internal abstract SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor);
		protected abstract bool EqualsCore(SyntaxTreeNode b);

		//equality members
		public bool Equals(SyntaxTreeNode other) {
			return this == other;
		}
		public override bool Equals(object obj) {
			return Equals(obj as SyntaxTreeNode);
		}
		public override int GetHashCode() {
			return base.GetHashCode(); //TODO
		}

		public static bool operator ==(SyntaxTreeNode a, SyntaxTreeNode b) {
			if (ReferenceEquals(a, b))
				return true;

			if (ReferenceEquals(a, null))
				return false;

			if (ReferenceEquals(b, null))
				return false;

			if (a.GetType() != b.GetType())
				return false;

			if (a.SubNodes.Count != b.SubNodes.Count)
				return false;

			if (!ReferenceEquals(a.SubNodes, b.SubNodes)) {
				for (int i = 0; i < a.SubNodes.Count; i++)
					if (a.SubNodes[i] != b.SubNodes[i])
						return false;
			}

			return a.EqualsCore(b);
		}
		public static bool operator !=(SyntaxTreeNode a, SyntaxTreeNode b) {
			return !(a == b);
		}
	}
}