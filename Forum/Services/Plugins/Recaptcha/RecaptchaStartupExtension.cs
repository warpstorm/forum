using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Services.Plugins.Recaptcha {
	public static class RecaptchaStartupExtension {
		public static IServiceCollection AddRecaptcha(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<Recaptcha2Options>(configuration);
			services.AddTransient<IRecaptcha2Validator, Recaptcha2Validator>();
			services.AddTransient<ValidateRecaptcha2ActionFilter>();

			services.Configure<Recaptcha3Options>(configuration);
			services.AddTransient<IRecaptcha3Validator, Recaptcha3Validator>();
			services.AddTransient<ValidateRecaptcha3ActionFilter>();

			return services;
		}
	}
}
