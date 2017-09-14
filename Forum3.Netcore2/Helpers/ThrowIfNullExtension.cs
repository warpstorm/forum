using System;

namespace Forum3.Helpers {
	/// Source: https://stackoverflow.com/questions/11522104
	public static class ThrowIfNullExtension {
		public static void ThrowIfNull<T>(this T o, string paramName) where T : class {
			if (o == null)
				throw new ArgumentNullException(paramName);
		}
	}
}