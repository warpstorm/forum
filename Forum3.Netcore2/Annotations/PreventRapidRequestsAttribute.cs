using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Forum3.Annotations {
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class PreventRapidRequestsAttribute : ActionFilterAttribute {
		public override void OnActionExecuting(ActionExecutingContext context) {
			if (!context.HttpContext.Request.Form.ContainsKey("__RequestVerificationToken"))
				return;

			var currentToken = context.HttpContext.Request.Form["__RequestVerificationToken"].ToString();
			var lastToken = context.HttpContext.Session.GetString(Constants.Keys.LastProcessedToken);

			if (lastToken == currentToken) {
				context.ModelState.AddModelError(string.Empty, "Looks like you accidentally submitted the same form twice.");
				return;
			}

			context.HttpContext.Session.SetString(Constants.Keys.LastProcessedToken, currentToken);

			var currentTime = DateTime.Now;
			var lastPostTimeStamp = context.HttpContext.Session.GetString(Constants.Keys.LastPostTimestamp);

			if (!string.IsNullOrEmpty(lastPostTimeStamp)) {
				var lastPostTime = Convert.ToDateTime(lastPostTimeStamp);

				if (currentTime < lastPostTime.AddSeconds(3)) {
					context.ModelState.AddModelError(string.Empty, "You're posting too fast.");
					return;
				}
			}

			context.HttpContext.Session.SetString(Constants.Keys.LastPostTimestamp, currentTime.ToString());
		}
	}
}