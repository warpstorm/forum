using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Plugins.Recaptcha {
	using ServiceModels = Models.ServiceModels;

	public static class RecaptchaStartupExtension {
		public static IServiceCollection AddRecaptcha(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<ServiceModels.RecaptchaOptions>(configuration);
			services.AddTransient<IRecaptchaValidator, RecaptchaValidator>();
			services.AddTransient<ValidateRecaptchaActionFilter>();

			return services;
		}
	}
}
