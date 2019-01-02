using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Plugins.Recaptcha {
	public static class RecaptchaStartupExtension {
		public static IServiceCollection AddRecaptcha(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<RecaptchaOptions>(configuration);
			services.AddTransient<IRecaptchaValidator, RecaptchaValidator>();
			services.AddTransient<ValidateRecaptchaActionFilter>();

			services.Configure<Recaptcha3Options>(configuration);
			services.AddTransient<IRecaptcha3Validator, Recaptcha3Validator>();
			services.AddTransient<ValidateRecaptcha3ActionFilter>();

			return services;
		}
	}
}
