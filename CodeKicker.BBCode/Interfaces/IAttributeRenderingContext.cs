using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeKicker.BBCode {
	public interface IAttributeRenderingContext {
		BBAttribute Attribute { get; }
		string AttributeValue { get; }
		string GetAttributeValueByID(string id);
	}
}
