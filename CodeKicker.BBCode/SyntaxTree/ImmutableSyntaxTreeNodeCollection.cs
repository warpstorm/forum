using CodeKicker.BBCode.Helpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CodeKicker.BBCode.SyntaxTree {
	public class ImmutableSyntaxTreeNodeCollection : ReadOnlyCollection<SyntaxTreeNode>, ISyntaxTreeNodeCollection {
		public ImmutableSyntaxTreeNodeCollection(IEnumerable<SyntaxTreeNode> list) : base(list.ToArray()) => list.ThrowIfNull(nameof(list));

		internal ImmutableSyntaxTreeNodeCollection(IList<SyntaxTreeNode> list, bool isFresh) : base(isFresh ? list : list.ToArray()) { }

		public static ImmutableSyntaxTreeNodeCollection Empty { get; } = new ImmutableSyntaxTreeNodeCollection(new SyntaxTreeNode[0], true);
	}
}