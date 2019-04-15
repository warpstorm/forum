using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Forum.Controllers.Annotations {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class SideLoadAttribute : ActionFilterAttribute {
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var sideLoaded = context.HttpContext.Request.Headers.ContainsKey("X-Requested-With")
						  && context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

			if (!sideLoaded) {
				context.ModelState.AddModelError("", "This action should only be called via XMLHttpRequest.");
			}

			await next();
		}
	}
}
