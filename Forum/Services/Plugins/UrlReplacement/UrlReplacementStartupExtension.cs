using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Plugins.UrlReplacement {
	public static class UrlReplacementStartupExtension {
		public static IServiceCollection AddUrlReplacement(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<ImgurClientModels.Options>(configuration.GetSection("Imgur"));
			services.AddTransient<ImgurClient>();
			services.AddTransient<YouTubeClient>();

			return services;
		}
	}
}
