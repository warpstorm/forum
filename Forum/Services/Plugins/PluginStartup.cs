using Forum.Services.Plugins.EmailSender;
using Forum.Services.Plugins.ImageStore;
using Forum.Services.Plugins.Recaptcha;
using Forum.Services.Plugins.UrlReplacement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Services.Plugins {
	public static class PluginStartup {
		public static IServiceCollection AddPlugins(this IServiceCollection services, IConfiguration configuration) {
			services.AddRecaptcha(configuration);
			services.AddImageStore(configuration);
			services.AddUrlReplacement(configuration);
			services.AddEmailSender(configuration);

			return services;
		}
	}
}
