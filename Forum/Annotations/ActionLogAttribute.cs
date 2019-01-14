using Forum.Contexts;
using Forum.Filters;
using Forum.Models.DataModels;
using Forum.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Forum.Annotations {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class ActionLogAttribute : ActionFilterAttribute {
		public bool IsReusable => false;
		public string Description { get; set; }

		public ActionLogAttribute(string description) {
			Description = description;
		}

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var service = context.HttpContext.RequestServices.GetRequiredService<ActionLogService>();

			var arguments = context.ActionArguments;

			foreach (ControllerParameterDescriptor parameter in context.ActionDescriptor.Parameters) {
				if (!arguments.ContainsKey(parameter.Name)) {
					arguments.Add(parameter.Name, parameter.ParameterInfo.DefaultValue);
				}
			}

			await service.Add(new ActionLogItem {
				Action = context.ActionDescriptor.RouteValues["action"],
				Controller = context.ActionDescriptor.RouteValues["controller"],
				Arguments = context.ActionArguments,
				Timestamp = DateTime.Now,
				Description = Description,
			});

			await next();
		}
	}
}
