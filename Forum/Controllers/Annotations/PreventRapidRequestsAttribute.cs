using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Forum.Annotations {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class PreventRapidRequestsAttribute : ActionFilterAttribute {
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			if (context.HttpContext.Request.Form.ContainsKey("__RequestVerificationToken")) {
				await context.HttpContext.Session.LoadAsync();

				var currentToken = context.HttpContext.Request.Form["__RequestVerificationToken"].ToString();
				var lastToken = context.HttpContext.Session.GetString(Constants.InternalKeys.LastProcessedToken);

				if (lastToken == currentToken) {
					context.ModelState.AddModelError(string.Empty, "Looks like you accidentally submitted the same form twice.");
				}

				var currentTime = DateTime.Now;
				var lastPostTimestamp = context.HttpContext.Session.GetString(Constants.InternalKeys.LastPostTimestamp);

				if (!string.IsNullOrEmpty(lastPostTimestamp)) {
					var lastTime = Convert.ToDateTime(lastPostTimestamp);

					if (currentTime < lastTime.AddSeconds(3)) {
						context.ModelState.AddModelError(string.Empty, "You're posting too fast.");
					}
				}

				// Only update the last timestamp if the state is still valid.
				if (context.ModelState.IsValid) {
					context.HttpContext.Session.SetString(Constants.InternalKeys.LastProcessedToken, currentToken);
					context.HttpContext.Session.SetString(Constants.InternalKeys.LastPostTimestamp, currentTime.ToString());
					await context.HttpContext.Session.CommitAsync();
				}
			}

			await next();
		}
	}
}