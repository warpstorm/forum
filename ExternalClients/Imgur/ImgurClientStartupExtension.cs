using Forum.ExternalClients.Imgur.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.ExternalClients.Imgur {
	public static class ImgurClientStartupExtension {
		public static IServiceCollection AddImgurClient(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<ImgurClientOptions>(configuration.GetSection("Imgur"));
			services.AddScoped<ImgurClient>();

			return services;
		}
	}
}
