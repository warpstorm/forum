using Microsoft.Extensions.DependencyInjection;

namespace Forum.ExternalClients.YouTube {
	public static class YouTubeClientStartupExtension {
		public static IServiceCollection AddYouTubeClient(this IServiceCollection services) {
			services.AddTransient<YouTubeClient>();
			return services;
		}
	}
}
