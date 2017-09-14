namespace Microsoft.AspNetCore.Mvc {
	public static class UrlHelperExtensions {
		public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme) {
			return urlHelper.Action(
				action: nameof(Forum3.Controllers.Authentication.ConfirmEmail),
				controller: nameof(Forum3.Controllers.Authentication),
				values: new { userId, code },
				protocol: scheme);
		}

		public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code, string scheme) {
			return urlHelper.Action(
				action: nameof(Forum3.Controllers.Authentication.ResetPassword),
				controller: nameof(Forum3.Controllers.Authentication),
				values: new { userId, code },
				protocol: scheme);
		}
	}
}