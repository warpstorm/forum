using System;
using Forum3.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forum3.Annotations {
	/// <summary>
	/// Allows local requests to not use HTTPS (i.e. development)
	/// </summary>
	public class RequireRemoteHttpsAttribute : RequireHttpsAttribute {
		public override void OnAuthorization(AuthorizationFilterContext filterContext) {
			if (filterContext == null)
				throw new ArgumentNullException(nameof(filterContext));

			if (filterContext.HttpContext.Request.IsLocal())
				return;

			base.OnAuthorization(filterContext);
		}
	}
}