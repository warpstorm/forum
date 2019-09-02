using Forum.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum.ExternalClients.SendGrid {
	public static class SendGridSenderClientStartupExtension {
		public static IServiceCollection AddSendGridSenderClient(this IServiceCollection services, IConfiguration configuration) {
			services.Configure<SendGridSenderClientOptions>(configuration);
			services.AddTransient<IEmailSender, SendGridSenderClient>();

			return services;
		}
	}
}
