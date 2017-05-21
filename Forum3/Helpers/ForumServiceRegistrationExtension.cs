using Microsoft.Extensions.DependencyInjection;
using Forum3.Services;

namespace Forum3.Helpers {
	public static class ForumServiceRegistrationExtension {
		public static IServiceCollection AddForum(this IServiceCollection services) {
			services.AddScoped<MessageService>();
			services.AddScoped<BoardService>();
			services.AddScoped<TopicService>();
			services.AddScoped<SiteSettingsService>();
			services.AddScoped<UserService>();

			return services;
		}
	}
}