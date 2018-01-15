using CodeKicker.BBCode.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CodeKicker.BBCode.SyntaxTree {
	public sealed class TagNode : SyntaxTreeNode {
		public BBTag Tag { get; }
		public IDictionary<BBAttribute, string> AttributeValues { get; }

		public TagNode(BBTag tag) : this(tag, null) { }
		public TagNode(BBTag tag, IList<SyntaxTreeNode> subNodes, IDictionary<BBAttribute, string> attributeValues = null) : base(subNodes) {
			tag.ThrowIfNull(nameof(tag));
			Tag = tag;

			if (attributeValues is null)
				AttributeValues = new Dictionary<BBAttribute, string>();
			else
				AttributeValues = new Dictionary<BBAttribute, string>(attributeValues);
		}

		public override string ToHtml() {
			var content = GetContent();
			return ReplaceAttributeValues(Tag.OpenTagTemplate, content) + (Tag.AutoRenderContent ? content : null) + ReplaceAttributeValues(Tag.CloseTagTemplate, content);
		}

		public override string ToBBCode() {
			var content = string.Concat(SubNodes.Select(s => s.ToBBCode()).ToArray());

			var attrs = "";
			var defAttr = Tag.FindAttribute(string.Empty);

			if (defAttr != null) {
				if (AttributeValues.ContainsKey(defAttr))
					attrs += "=" + AttributeValues[defAttr];
			}

			foreach (var attrKvp in AttributeValues) {
				if (attrKvp.Key.Name == "")
					continue;
				attrs += " " + attrKvp.Key.Name + "=" + attrKvp.Value;
			}

			return "[" + Tag.Name + attrs + "]" + content + "[/" + Tag.Name + "]";
		}

		public override string ToText() => string.Concat(SubNodes.Select(s => s.ToText()).ToArray());

		string GetContent() {
			var content = string.Concat(SubNodes.Select(s => s.ToHtml()).ToArray());
			return Tag.ContentTransformer is null ? content : Tag.ContentTransformer(content);
		}

		string ReplaceAttributeValues(string template, string content) {
			var attributesWithValues = (from attr in Tag.Attributes
										group attr by attr.ID into gAttrByID
										let val = (from attr in gAttrByID
												   let val = TryGetValue(attr)
												   where val != null
												   select new { attr, val }).FirstOrDefault()
										select new { attrID = gAttrByID.Key, attrAndVal = val }).ToList();

			var attrValuesByID = attributesWithValues.Where(x => x.attrAndVal != null).ToDictionary(x => x.attrID, x => x.attrAndVal.val);

			if (!attrValuesByID.ContainsKey(BBTag.ContentPlaceholderName))
				attrValuesByID.Add(BBTag.ContentPlaceholderName, content);

			var output = template;

			foreach (var x in attributesWithValues) {
				var placeholderStr = "${" + x.attrID + "}";

				if (x.attrAndVal != null) {
					//replace attributes with values
					var rawValue = x.attrAndVal.val;
					var attribute = x.attrAndVal.attr;
					output = ReplaceAttribute(output, attribute, rawValue, placeholderStr, attrValuesByID);
				}
			}

			//replace empty attributes
			var attributeIDsWithValues = new HashSet<string>(attributesWithValues.Where(x => x.attrAndVal != null).Select(x => x.attrID));
			var emptyAttributes = Tag.Attributes.Where(attr => !attributeIDsWithValues.Contains(attr.ID)).ToList();

			foreach (var attr in emptyAttributes) {
				var placeholderStr = "${" + attr.ID + "}";
				output = ReplaceAttribute(output, attr, null, placeholderStr, attrValuesByID);
			}

			output = output.Replace("${" + BBTag.ContentPlaceholderName + "}", content);

			return output;
		}

		static string ReplaceAttribute(string output, BBAttribute attribute, string rawValue, string placeholderStr, Dictionary<string, string> attrValuesByID) {
			string effectiveValue;

			if (attribute.ContentTransformer is null) {
				effectiveValue = rawValue;
			}
			else {
				var ctx = new AttributeRenderingContextImpl(attribute, rawValue, attrValuesByID);

				effectiveValue = attribute.ContentTransformer(ctx);
			}

			if (effectiveValue is null)
				effectiveValue = "";

			var encodedValue =
				attribute.HtmlEncodingMode == EHtmlEncodingMode.HtmlAttributeEncode ? WebUtility.HtmlEncode(effectiveValue)
					: attribute.HtmlEncodingMode == EHtmlEncodingMode.HtmlEncode ? WebUtility.HtmlEncode(effectiveValue)
						  : effectiveValue;

			output = output.Replace(placeholderStr, encodedValue);

			return output;
		}

		string TryGetValue(BBAttribute attr) {
			AttributeValues.TryGetValue(attr, out var val);
			return val;
		}

		public override SyntaxTreeNode SetSubNodes(IList<SyntaxTreeNode> subNodes) {
			subNodes.ThrowIfNull(nameof(subNodes));
			return new TagNode(Tag, subNodes, AttributeValues);
		}

		internal override SyntaxTreeNode AcceptVisitor(SyntaxTreeVisitor visitor) {
			visitor.ThrowIfNull(nameof(visitor));
			return visitor.Visit(this);
		}

		protected override bool EqualsCore(SyntaxTreeNode b) {
			var casted = (TagNode) b;

			return
				Tag == casted.Tag
				&& AttributeValues.All(attr => casted.AttributeValues[attr.Key] == attr.Value)
				&& casted.AttributeValues.All(attr => AttributeValues[attr.Key] == attr.Value);
		}

		class AttributeRenderingContextImpl : IAttributeRenderingContext {
			public BBAttribute Attribute { get; }
			public string AttributeValue { get; }
			public IDictionary<string, string> GetAttributeValueByIDData { get; }

			public AttributeRenderingContextImpl(BBAttribute attribute, string attributeValue, IDictionary<string, string> getAttributeValueByIdData) {
				Attribute = attribute;
				AttributeValue = attributeValue;
				GetAttributeValueByIDData = getAttributeValueByIdData;
			}

			public string GetAttributeValueByID(string id) {
				if (!GetAttributeValueByIDData.TryGetValue(id, out var value))
					return null;

				return value;
			}
		}
	}
}