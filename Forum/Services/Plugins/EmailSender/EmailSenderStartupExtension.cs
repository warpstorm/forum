using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.Services.Plugins.EmailSender {
	public static class EmailSenderStartupExtension {
		public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<EmailSenderOptions>(configuration);
			services.AddTransient<IEmailSender, EmailSender>();

			return services;
		}
	}
}
