using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeKicker.BBCode.SyntaxTree {
	public class SyntaxTreeNodeCollection : Collection<SyntaxTreeNode>, ISyntaxTreeNodeCollection {
		public SyntaxTreeNodeCollection() : base() { }
		public SyntaxTreeNodeCollection(IList<SyntaxTreeNode> list) : base(list ?? new List<SyntaxTreeNode>()) { }

		protected override void SetItem(int index, SyntaxTreeNode item) {
			if (item == null)
				throw new ArgumentNullException("item");

			base.SetItem(index, item);
		}
		protected override void InsertItem(int index, SyntaxTreeNode item) {
			if (item == null)
				throw new ArgumentNullException("item");

			base.InsertItem(index, item);
		}
	}
}
