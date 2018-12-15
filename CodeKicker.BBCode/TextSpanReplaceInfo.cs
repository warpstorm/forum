using CodeKicker.BBCode.SyntaxTree;
using System;

namespace CodeKicker.BBCode {
	public class TextSpanReplaceInfo {
		public int Index { get; }
		public int Length { get; }
		public SyntaxTreeNode Replacement { get; }

		public TextSpanReplaceInfo(int index, int length, SyntaxTreeNode replacement) {
			if (index < 0) {
				throw new ArgumentOutOfRangeException("index");
			}

			if (length < 0) {
				throw new ArgumentOutOfRangeException("index");
			}

			Index = index;
			Length = length;
			Replacement = replacement;
		}
	}
}