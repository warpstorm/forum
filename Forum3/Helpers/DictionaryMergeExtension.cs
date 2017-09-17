using System.Collections.Generic;
using System.Linq;

namespace Forum3.Helpers {
	// Source: https://stackoverflow.com/a/2679857/2621693
	public static class DictionaryMergeExtension {
		public static T Merge<T, K, V>(this T to, params IDictionary<K, V>[] sources) where T : IDictionary<K, V>, new() {
			var result = new T();
			var sourceCollection = (new List<IDictionary<K, V>> { to }).Concat(sources);

			foreach (var source in sourceCollection)
				foreach (var kvp in source)
					result[kvp.Key] = kvp.Value;

			return result;
		}
	}
}