using Forum.Services.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Forum.Plugins.Recaptcha {
	public class ValidateRecaptcha3ActionFilter : IAsyncAuthorizationFilter {
		IRecaptcha3Validator RecaptchaValidator { get; }

		public ValidateRecaptcha3ActionFilter(
			IRecaptcha3Validator recaptchaValidator
		) {
			RecaptchaValidator = recaptchaValidator;
		}

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
			if (!context.HttpContext.Request.IsLocal()) {
				var form = await context.HttpContext.Request.ReadFormAsync();
				var recaptchaResponse = form["g-recaptcha-response"];
				var ipAddress = context.HttpContext.Connection?.RemoteIpAddress?.ToString();

				if (string.IsNullOrEmpty(recaptchaResponse)) {
					context.ModelState.AddModelError("g-recaptcha-response", "The recaptcha response appears empty.");
				}

				try {
					await RecaptchaValidator.Validate(recaptchaResponse, ipAddress);
				}
				catch (Exception e) {
					context.ModelState.AddModelError("g-recaptcha-response", e.Message);
				}
			}
		}
	}
}