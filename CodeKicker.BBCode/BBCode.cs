using CodeKicker.BBCode.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeKicker.BBCode {
	public static class BBCode {
		static readonly BBCodeParser defaultParser = GetParser();

		/// <summary>
		/// Transforms the given BBCode into safe HTML with the default configuration from http://codekicker.de
		/// </summary>
		/// <remarks>
		/// This method is thread safe.
		/// </remarks>
		/// <param name="bbCode">A non-null string of valid BBCode.</param>
		/// <returns></returns>
		public static string ToHtml(string bbCode) {
			if (bbCode == null)
				throw new ArgumentNullException("bbCode");

			return defaultParser.ToHtml(bbCode);
		}

		static BBCodeParser GetParser() {
			return new BBCodeParser(EErrorMode.ErrorFree, null, new[]
				{
					new BBTag("b", "<b>", "</b>"),
					new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
					new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
					new BBTag("code", "<pre class=\"prettyprint\">", "</pre>"),
					new BBTag("img", "<img src=\"${content}\" />", "", false, true),
					new BBTag("quote", "<blockquote>", "</blockquote>"),
					new BBTag("list", "<ul>", "</ul>"),
					new BBTag("*", "<li>", "</li>", true, false),
					new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
				});
		}

		public static readonly string InvalidBBCodeTextChars = @"[]\";

		/// <summary>
		/// Encodes an arbitrary string to be valid BBCode. Example: "[b]" => "\[b\]". The resulting string is safe against BBCode-Injection attacks.
		/// </summary>
		public static string EscapeText(string text) {
			if (text == null)
				throw new ArgumentNullException("text");

			var escapeCount = 0;

			for (int i = 0; i < text.Length; i++) {
				if (text[i] == '[' || text[i] == ']' || text[i] == '\\')
					escapeCount++;
			}

			if (escapeCount == 0)
				return text;

			var output = new char[text.Length + escapeCount];
			var outputWritePos = 0;

			for (int i = 0; i < text.Length; i++) {
				if (text[i] == '[' || text[i] == ']' || text[i] == '\\')
					output[outputWritePos++] = '\\';

				output[outputWritePos++] = text[i];
			}

			return new string(output);
		}

		/// <summary>
		/// Decodes a string of BBCode that only contains text (no tags). Example: "\[b\]" => "[b]". This is the reverse oepration of EscapeText.
		/// </summary>
		public static string UnescapeText(string text) {
			if (text == null)
				throw new ArgumentNullException("text");

			return text.Replace("\\[", "[").Replace("\\]", "]").Replace("\\\\", "\\");
		}

		public static SyntaxTreeNode ReplaceTextSpans(SyntaxTreeNode node, Func<string, IList<TextSpanReplaceInfo>> getTextSpansToReplace, Func<TagNode, bool> tagFilter) {
			if (node == null)
				throw new ArgumentNullException("node");

			if (getTextSpansToReplace == null)
				throw new ArgumentNullException("getTextSpansToReplace");

			if (node is TextNode) {
				var text = ((TextNode)node).Text;

				var replacements = getTextSpansToReplace(text);

				if (replacements == null || replacements.Count == 0)
					return node;

				var replacementNodes = new List<SyntaxTreeNode>(replacements.Count * 2 + 1);
				var lastPos = 0;

				foreach (var r in replacements) {
					if (r.Index < lastPos)
						throw new ArgumentException("the replacement text spans must be ordered by index and non-overlapping");

					if (r.Index > text.Length - r.Length)
						throw new ArgumentException("every replacement text span must reference a range within the text node");

					if (r.Index != lastPos)
						replacementNodes.Add(new TextNode(text.Substring(lastPos, r.Index - lastPos)));

					if (r.Replacement != null)
						replacementNodes.Add(r.Replacement);

					lastPos = r.Index + r.Length;
				}

				if (lastPos != text.Length)
					replacementNodes.Add(new TextNode(text.Substring(lastPos)));

				return new SequenceNode(replacementNodes);
			}
			else {
				var fixedSubNodes = node.SubNodes.Select(n => {
					//skip filtered tags
					if (n is TagNode && (tagFilter != null && !tagFilter((TagNode)n)))
						return n;

					var repl = ReplaceTextSpans(n, getTextSpansToReplace, tagFilter);

					Debug.Assert(repl != null);

					return repl;
				}).ToList();

				if (fixedSubNodes.SequenceEqual(node.SubNodes, ReferenceEqualityComparer<SyntaxTreeNode>.Instance))
					return node;

				return node.SetSubNodes(fixedSubNodes);
			}
		}

		public static void VisitTextNodes(SyntaxTreeNode node, Action<string> visitText, Func<TagNode, bool> tagFilter) {
			if (node == null)
				throw new ArgumentNullException("node");

			if (visitText == null)
				throw new ArgumentNullException("visitText");

			if (node is TextNode) {
				visitText(((TextNode)node).Text);
			}
			else {
				//skip filtered tags
				if (node is TagNode && (tagFilter != null && !tagFilter((TagNode)node)))
					return;

				foreach (var subNode in node.SubNodes)
					VisitTextNodes(subNode, visitText, tagFilter);
			}
		}

		class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class {
			public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

			public bool Equals(T x, T y) {
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(T obj) {
				return obj == null ? 0 : obj.GetHashCode();
			}
		}
	}
}