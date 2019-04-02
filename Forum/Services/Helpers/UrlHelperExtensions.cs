using Forum.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Services.Helpers {
	public static class UrlHelperExtensions {
		public static string AbsoluteAction(this IUrlHelper url, string actionName, string controllerName, object routeValues = null) {
			var scheme = url.ActionContext.HttpContext.Request.Scheme;
			return url.Action(actionName, controllerName, routeValues, scheme);
		}

		public static string DisplayMessage(this IUrlHelper url, int topicId, int messageId) => url.Action(nameof(Topics.Display), nameof(Topics), new { id = topicId, page = 1, target = messageId }) + $"#message{messageId}";
	}
}