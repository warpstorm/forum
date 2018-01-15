using CodeKicker.BBCode.Helpers;
using CodeKicker.BBCode.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeKicker.BBCode {
	/// <summary>
	/// This class is useful for creating a custom parser. You can customize which tags are available and how they are translated to HTML.
	/// </summary>
	public class BBCodeParser {
		public IList<BBTag> Tags { get; }
		public string TextNodeHtmlTemplate { get; }
		public EErrorMode ErrorMode { get; }

		public BBCodeParser(IList<BBTag> tags) : this(EErrorMode.ErrorFree, null, tags) { }

		public BBCodeParser(EErrorMode errorMode, string textNodeHtmlTemplate, IList<BBTag> tags) {
			if (!Enum.IsDefined(typeof(EErrorMode), errorMode))
				throw new ArgumentOutOfRangeException("errorMode");

			tags.ThrowIfNull(nameof(tags));

			ErrorMode = errorMode;
			TextNodeHtmlTemplate = textNodeHtmlTemplate;
			Tags = tags;
		}

		public virtual string ToHtml(string bbCode) {
			bbCode.ThrowIfNull(nameof(bbCode));
			return ParseSyntaxTree(bbCode).ToHtml();
		}

		public virtual SequenceNode ParseSyntaxTree(string bbCode) {
			bbCode.ThrowIfNull(nameof(bbCode));

			var stack = new Stack<SyntaxTreeNode>();
			var rootNode = new SequenceNode();
			stack.Push(rootNode);

			var end = 0;

			while (end < bbCode.Length) {
				if (MatchTagEnd(bbCode, ref end, stack))
					continue;

				if (MatchStartTag(bbCode, ref end, stack))
					continue;

				if (MatchTextNode(bbCode, ref end, stack))
					continue;

				//there is no possible match at the current position
				if (ErrorMode != EErrorMode.ErrorFree)
					throw new BBCodeParsingException(string.Empty);

				//if the error free mode is enabled force interpretation as text if no other match could be made
				AppendText(bbCode[end].ToString(), stack);

				end++;
			}

			//assert bbCode was matched entirely
			Debug.Assert(end == bbCode.Length);

			//close all tags that are still open and can be closed implicitly
			while (stack.Count > 1) {
				var node = (TagNode) stack.Pop();

				if (node.Tag.RequiresClosingTag && ErrorMode == EErrorMode.Strict)
					throw new BBCodeParsingException(MessagesHelper.GetString("TagNotClosed", node.Tag.Name));
			}

			if (stack.Count != 1) {
				Debug.Assert(ErrorMode != EErrorMode.ErrorFree);
				throw new BBCodeParsingException(string.Empty); //only the root node may be left
			}

			return rootNode;
		}

		bool MatchTagEnd(string bbCode, ref int pos, Stack<SyntaxTreeNode> stack) {
			var end = pos;

			var tagEnd = ParseTagEnd(bbCode, ref end);

			if (tagEnd is null)
				return false;

			while (true) {
				//could also be a SequenceNode
				var openingNode = stack.Peek() as TagNode;

				if (openingNode is null && ErrorOrReturn("TagNotOpened", tagEnd))
					return false;

				//ErrorOrReturn will either or throw make this stack frame exit
				Debug.Assert(openingNode != null);

				if (!openingNode.Tag.Name.Equals(tagEnd, StringComparison.OrdinalIgnoreCase)) {
					//a nesting imbalance was detected

					if (openingNode.Tag.RequiresClosingTag && ErrorOrReturn("TagNotMatching", tagEnd, openingNode.Tag.Name))
						return false;
					else
						stack.Pop();
				}
				else {
					//the opening node properly matches the closing node
					stack.Pop();
					break;
				}
			}

			pos = end;

			return true;
		}

		bool MatchStartTag(string bbCode, ref int pos, Stack<SyntaxTreeNode> stack) {
			var end = pos;
			var tag = ParseTagStart(bbCode, ref end);

			if (tag is null)
				return false;

			if (tag.Tag.EnableIterationElementBehavior) {
				//this element behaves like a list item: it allows tags as content, it auto-closes and it does not nest.
				//the last property is ensured by closing all currently open tags up to the opening list element

				var isThisTagAlreadyOnStack = stack.OfType<TagNode>().Any(n => n.Tag == tag.Tag);
				//if this condition is false, no nesting would occur anyway

				if (isThisTagAlreadyOnStack) {
					//could also be a SequenceNode
					var openingNode = stack.Peek() as TagNode;

					//isThisTagAlreadyOnStack would have been false
					Debug.Assert(openingNode != null);

					if (openingNode.Tag != tag.Tag && ErrorMode == EErrorMode.Strict && ErrorOrReturn("TagNotMatching", tag.Tag.Name, openingNode.Tag.Name))
						return false;

					while (true) {
						var poppedOpeningNode = (TagNode) stack.Pop();

						if (poppedOpeningNode.Tag != tag.Tag) {
							//a nesting imbalance was detected

							if (openingNode.Tag.RequiresClosingTag && ErrorMode == EErrorMode.Strict && ErrorOrReturn("TagNotMatching", tag.Tag.Name, openingNode.Tag.Name))
								return false;

							//close the (wrongly) open tag. we have already popped so do nothing.
						}
						else {
							//the opening node matches the closing node
							//close the already open li-item. we have already popped. we have already popped so do nothing.
							break;
						}
					}
				}
			}

			stack.Peek().SubNodes.Add(tag);

			//leaf elements have no content - they are closed immediately
			if (tag.Tag.TagClosingStyle != EBBTagClosingStyle.LeafElementWithoutContent)
				stack.Push(tag);

			pos = end;

			return true;
		}

		bool MatchTextNode(string bbCode, ref int pos, Stack<SyntaxTreeNode> stack) {
			var end = pos;

			var textNode = ParseText(bbCode, ref end);

			if (textNode != null) {
				AppendText(textNode, stack);
				pos = end;
				return true;
			}

			return false;
		}

		void AppendText(string textToAppend, Stack<SyntaxTreeNode> stack) {
			var currentNode = stack.Peek();
			var lastChild = currentNode.SubNodes.Count == 0 ? null : currentNode.SubNodes[currentNode.SubNodes.Count - 1] as TextNode;

			TextNode newChild;
			if (lastChild is null)
				newChild = new TextNode(textToAppend, TextNodeHtmlTemplate);
			else
				newChild = new TextNode(lastChild.Text + textToAppend, TextNodeHtmlTemplate);

			if (currentNode.SubNodes.Count != 0 && currentNode.SubNodes[currentNode.SubNodes.Count - 1] is TextNode)
				currentNode.SubNodes[currentNode.SubNodes.Count - 1] = newChild;
			else
				currentNode.SubNodes.Add(newChild);
		}

		TagNode ParseTagStart(string input, ref int pos) {
			var end = pos;

			if (!ParseChar(input, ref end, '['))
				return null;

			var tagName = ParseName(input, ref end);

			if (tagName is null)
				return null;

			var tag = Tags.SingleOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

			if (tag is null && ErrorOrReturn("UnknownTag", tagName))
				return null;

			var result = new TagNode(tag);

			var defaultAttrValue = ParseAttributeValue(input, ref end);

			if (defaultAttrValue != null) {
				var attr = tag.FindAttribute(string.Empty);

				if (attr is null && ErrorOrReturn("UnknownAttribute", tag.Name, "\"Default Attribute\""))
					return null;

				result.AttributeValues.Add(attr, defaultAttrValue);
			}

			while (true) {
				ParseWhitespace(input, ref end);

				var attrName = ParseName(input, ref end);

				if (attrName is null)
					break;

				var attrVal = ParseAttributeValue(input, ref end);

				if (attrVal is null && ErrorOrReturn(string.Empty))
					return null;

				if (tag.Attributes is null && ErrorOrReturn("UnknownTag", tag.Name))
					return null;

				var attr = tag.FindAttribute(attrName);

				if (attr is null && ErrorOrReturn("UnknownTag", tag.Name, attrName))
					return null;

				if (result.AttributeValues.ContainsKey(attr) && ErrorOrReturn("DuplicateAttribute", tagName, attrName))
					return null;

				result.AttributeValues.Add(attr, attrVal);
			}

			if (!ParseChar(input, ref end, ']') && ErrorOrReturn("TagNotClosed", tagName))
				return null;

			pos = end;

			return result;
		}

		string ParseTagEnd(string input, ref int pos) {
			var end = pos;

			if (!ParseChar(input, ref end, '['))
				return null;

			if (!ParseChar(input, ref end, '/'))
				return null;

			var tagName = ParseName(input, ref end);

			if (tagName is null)
				return null;

			ParseWhitespace(input, ref end);

			if (!ParseChar(input, ref end, ']')) {
				if (ErrorMode == EErrorMode.ErrorFree)
					return null;
				else
					throw new BBCodeParsingException(string.Empty);
			}

			pos = end;
			return tagName;
		}

		string ParseText(string input, ref int pos) {
			var end = pos;
			var escapeFound = false;
			var anyEscapeFound = false;

			while (end < input.Length) {
				if (input[end] == '[' && !escapeFound)
					break;

				if (ErrorMode == EErrorMode.Strict && input[end] == ']' && !escapeFound)
					throw new BBCodeParsingException(MessagesHelper.GetString("NonescapedChar"));

				if (input[end] == '\\' && !escapeFound) {
					escapeFound = true;
					anyEscapeFound = true;
				}
				else if (escapeFound) {
					if (ErrorMode == EErrorMode.Strict && !(input[end] == '[' || input[end] == ']' || input[end] == '\\'))
						throw new BBCodeParsingException(MessagesHelper.GetString("EscapeChar"));

					escapeFound = false;
				}

				end++;
			}

			if (ErrorMode == EErrorMode.Strict && escapeFound)
				throw new BBCodeParsingException(string.Empty);

			var result = input.Substring(pos, end - pos);

			if (anyEscapeFound) {
				var result2 = new char[result.Length];
				var writePos = 0;
				var lastWasEscapeChar = false;

				for (var i = 0; i < result.Length; i++) {
					if (!lastWasEscapeChar && result[i] == '\\') {
						if (i < result.Length - 1) {
							if (!(result[i + 1] == '[' || result[i + 1] == ']' || result[i + 1] == '\\'))
								result2[writePos++] = result[i]; //the next char was not escapable. write the slash into the output array
							else
								lastWasEscapeChar = true; //the next char is meant to be escaped so the backslash is skipped
						}
						else {
							result2[writePos++] = '\\'; //the backslash was the last char in the string. just write it into the output array
						}
					}
					else {
						result2[writePos++] = result[i];
						lastWasEscapeChar = false;
					}
				}

				result = new string(result2, 0, writePos);
			}

			pos = end;

			return string.IsNullOrEmpty(result) ? null : result;
		}

		static string ParseName(string input, ref int pos) {
			var end = pos;

			for (; end < input.Length && (char.ToLower(input[end]) >= 'a' && char.ToLower(input[end]) <= 'z' || (input[end]) >= '0' && (input[end]) <= '9' || input[end] == '*'); end++)
				;

			if (end - pos == 0)
				return null;

			var result = input.Substring(pos, end - pos);

			pos = end;

			return result;
		}

		static string ParseAttributeValue(string input, ref int pos) {
			var end = pos;

			if (end >= input.Length || input[end] != '=')
				return null;

			end++;

			var endIndex = input.IndexOfAny(" []".ToCharArray(), end);

			if (endIndex == -1)
				endIndex = input.Length;

			var valStart = pos + 1;
			var result = input.Substring(valStart, endIndex - valStart);

			pos = endIndex;

			return result;
		}

		static bool ParseWhitespace(string input, ref int pos) {
			var end = pos;

			while (end < input.Length && char.IsWhiteSpace(input[end]))
				end++;

			var found = pos != end;

			pos = end;

			return found;
		}

		static bool ParseChar(string input, ref int pos, char c) {
			if (pos >= input.Length || input[pos] != c)
				return false;

			pos++;

			return true;
		}

		bool ErrorOrReturn(string msgKey, params string[] parameters) {
			if (ErrorMode == EErrorMode.ErrorFree)
				return true;
			else
				throw new BBCodeParsingException(string.IsNullOrEmpty(msgKey) ? "" : MessagesHelper.GetString(msgKey, parameters));
		}
	}
}