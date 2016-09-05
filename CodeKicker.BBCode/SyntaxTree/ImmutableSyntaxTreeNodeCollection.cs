using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CodeKicker.BBCode.SyntaxTree {
	public class ImmutableSyntaxTreeNodeCollection : ReadOnlyCollection<SyntaxTreeNode>, ISyntaxTreeNodeCollection {
		static readonly ImmutableSyntaxTreeNodeCollection empty = new ImmutableSyntaxTreeNodeCollection(new SyntaxTreeNode[0], true);

		public ImmutableSyntaxTreeNodeCollection(IEnumerable<SyntaxTreeNode> list) : base(list.ToArray()) {
			if (list == null)
				throw new ArgumentNullException("list");
		}
		internal ImmutableSyntaxTreeNodeCollection(IList<SyntaxTreeNode> list, bool isFresh) : base(isFresh ? list : list.ToArray()) { }

		public static ImmutableSyntaxTreeNodeCollection Empty {
			get { return empty; }
		}
	}
}
