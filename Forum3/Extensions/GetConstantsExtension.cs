using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Forum3.Extensions {
	public static class GetConstantsExtension {
		// Inspired by https://stackoverflow.com/a/10261848/2621693
		public static IEnumerable<string> GetConstants(this Type type) {
			var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			var constants = fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();
			return constants.Select(c => c.Name);
		}
	}
}