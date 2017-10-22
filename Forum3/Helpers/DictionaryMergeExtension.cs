using System.Collections.Generic;
using System.Linq;

namespace Forum3.Helpers {
	public static class DictionaryMergeExtension {
		// Inspired by: https://stackoverflow.com/a/2679857/2621693
		public static void Merge<T, K, V>(this T to, params IDictionary<K, V>[] sources) where T : IDictionary<K, V>, new() {
			var sourceCollection = (new List<IDictionary<K, V>> { to }).Concat(sources);

			foreach (var source in sourceCollection)
				foreach (var kvp in source)
					to[kvp.Key] = kvp.Value;
		}
	}
}