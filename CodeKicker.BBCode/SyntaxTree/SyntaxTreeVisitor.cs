namespace CodeKicker.BBCode.SyntaxTree {
	public class SyntaxTreeVisitor {
		public SyntaxTreeNode Visit(SyntaxTreeNode node) {
			if (node == null)
				return null;

			return node.AcceptVisitor(this);
		}

		protected internal virtual SyntaxTreeNode Visit(SequenceNode node) {
			if (node == null) return null;

			var modifiedSubNodes = GetModifiedSubNodes(node);

			if (modifiedSubNodes == null)
				//unmodified
				return node;
			else
				//subnodes were modified
				return node.SetSubNodes(modifiedSubNodes);
		}

		protected internal virtual SyntaxTreeNode Visit(TagNode node) {
			if (node == null) return null;

			var modifiedSubNodes = GetModifiedSubNodes(node);

			if (modifiedSubNodes == null)
				//unmodified
				return node;
			else
				//subnodes were modified
				return node.SetSubNodes(modifiedSubNodes);
		}

		protected internal virtual SyntaxTreeNode Visit(TextNode node) {
			return node;
		}

		SyntaxTreeNodeCollection GetModifiedSubNodes(SyntaxTreeNode node) {
			//lazy init
			SyntaxTreeNodeCollection modifiedSubNodes = null;

			for (int i = 0; i < node.SubNodes.Count; i++) {
				var subNode = node.SubNodes[i];

				var replacement = Visit(subNode);

				if (replacement != subNode) {
					//lazy init
					if (modifiedSubNodes == null) {
						modifiedSubNodes = new SyntaxTreeNodeCollection();

						//copy unmodified nodes
						for (int j = 0; j < i; j++)
							modifiedSubNodes.Add(node.SubNodes[j]);
					}

					//insert replacement
					if (replacement != null)
						modifiedSubNodes.Add(replacement);
				}
				else {
					//only insert unmodified subnode if the lazy collection has been initialized
					if (modifiedSubNodes != null)
						modifiedSubNodes.Add(subNode);
				}
			}

			return modifiedSubNodes;
		}
	}
}
