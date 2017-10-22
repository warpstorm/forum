namespace CodeKicker.BBCode {
	public interface IAttributeRenderingContext {
		BBAttribute Attribute { get; }
		string AttributeValue { get; }
		string GetAttributeValueByID(string id);
	}
}