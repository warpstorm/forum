using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Plugins.Recaptcha {
	public static class RecaptchaStartupExtension {
		public static IServiceCollection AddRecaptcha(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<RecaptchaOptions>(configuration);
			services.AddTransient<IRecaptchaValidator, RecaptchaValidator>();
			services.AddTransient<ValidateRecaptchaActionFilter>();

			return services;
		}
	}
}
