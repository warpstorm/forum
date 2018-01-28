using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Extensions {
	/// Source: https://stackoverflow.com/questions/11522104
	public static class ThrowIfNullExtension {
		public static void ThrowIfNull<T>(this T o, string paramName) where T : class {
			if (o is null)
				throw new ArgumentNullException(paramName);

			if (o is string && string.IsNullOrEmpty(o.ToString()))
				throw new ArgumentNullException(paramName);
		}

		public static void ThrowIfEmpty<T>(this IEnumerable<T> o, string name) 
			where T : class {

			if (!o.Any())
				throw new ArgumentException($"{name} is an empty collection.");
		}
	}
}