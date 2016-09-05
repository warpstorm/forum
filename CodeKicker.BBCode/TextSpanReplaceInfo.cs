using System;
using CodeKicker.BBCode.SyntaxTree;

namespace CodeKicker.BBCode {
	public class TextSpanReplaceInfo {
		public int Index { get; private set; }
		public int Length { get; private set; }
		public SyntaxTreeNode Replacement { get; private set; }

		public TextSpanReplaceInfo(int index, int length, SyntaxTreeNode replacement) {
			if (index < 0) throw new ArgumentOutOfRangeException("index");
			if (length < 0) throw new ArgumentOutOfRangeException("index");

			Index = index;
			Length = length;
			Replacement = replacement;
		}
	}
}