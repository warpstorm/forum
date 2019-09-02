using Forum.Core.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace Forum.ExternalClients.Recaptcha {
	public class ValidateRecaptcha2ActionFilter : IAsyncAuthorizationFilter {
		IRecaptcha2Validator RecaptchaValidator { get; }

		public ValidateRecaptcha2ActionFilter(IRecaptcha2Validator recaptchaValidator) => RecaptchaValidator = recaptchaValidator;

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
			if (context.HttpContext.Request.IsLocal()) {
				return;
			}

			var form = await context.HttpContext.Request.ReadFormAsync();
			var recaptchaResponse = form["g-recaptcha-response"];
			var ipAddress = context.HttpContext.Connection?.RemoteIpAddress?.ToString();

			if (string.IsNullOrEmpty(recaptchaResponse)) {
				context.ModelState.AddModelError("g-recaptcha-response", RecaptchaValidator.Response);
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