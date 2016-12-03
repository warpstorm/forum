using Forum3.Interfaces.Users;
using Forum3.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Forum3.Helpers {
	public static class ForumServiceRegistrationExtension {
		public static IServiceCollection AddForum(this IServiceCollection services) {
			services.AddTransient<IEmailSender, AuthMessageSender>();
			services.AddTransient<ISmsSender, AuthMessageSender>();

			services.AddScoped<MessageService>();
			services.AddScoped<BoardService>();
			services.AddScoped<TopicService>();
			services.AddScoped<SiteSettingsService>();
			services.AddScoped<UserService>();

			return services;
		}
    }
}
