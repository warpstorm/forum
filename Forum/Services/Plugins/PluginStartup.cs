using Forum.Plugins.EmailSender;
using Forum.Plugins.ImageStore;
using Forum.Plugins.Recaptcha;
using Forum.Plugins.UrlReplacement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Plugins {
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
