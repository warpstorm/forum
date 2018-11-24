using Forum.Errors;
using System.Collections.Generic;
using System.Linq;

namespace Forum.Extensions {
	/// Source: https://stackoverflow.com/questions/11522104
	public static class ThrowIfNullExtension {
		public static void ThrowIfNull<T>(this T o, string paramName) where T : class {
			if (o is null)
				throw new HttpBadRequestError($"The parameter '{paramName}' cannot be null.");

			if (o is string && string.IsNullOrEmpty(o.ToString()))
				throw new HttpBadRequestError($"The parameter '{paramName}' cannot be null.");
		}

		public static void ThrowIfEmpty<T>(this IEnumerable<T> o, string paramName) 
			where T : class {

			if (!o.Any())
				throw new HttpBadRequestError($"The parameter '{paramName}' cannot be an empty collection.");
		}
	}
}