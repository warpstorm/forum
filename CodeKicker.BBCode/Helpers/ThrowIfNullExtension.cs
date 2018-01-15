using System;

namespace CodeKicker.BBCode.Helpers {
	/// Source: https://stackoverflow.com/questions/11522104
	public static class ThrowIfNullExtension {
		public static void ThrowIfNull<T>(this T o, string paramName, bool allowEmptyStrings = false) where T : class {
			if (o == null)
				throw new ArgumentNullException(paramName);

			if (o is string && string.IsNullOrEmpty(o.ToString()) && !allowEmptyStrings)
				throw new ArgumentNullException(paramName);
		}
	}
}