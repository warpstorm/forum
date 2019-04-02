using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Forum.Extensions {
	public static class AddModelErrorsExtension {
		public static void AddModelErrors(this ModelStateDictionary modelState, Dictionary<string, string> errors, string prefix = "") {
			foreach (var error in errors) {
				var key = prefix + error.Key;
				modelState.AddModelError(key, error.Value);
			}
		}
	}
}
