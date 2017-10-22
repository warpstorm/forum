using Forum3.Interfaces.Users;
using Forum3.Models.ServiceModels;
using Forum3.Services;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;

namespace Forum3.Helpers {
	public static class ForumServiceRegistrationExtension {
		public static IServiceCollection AddForum(this IServiceCollection services, IConfiguration configuration) {
			AddTransientServices(services, configuration);
			AddScopedServices(services, configuration);
			AddSingletonServices(services, configuration);

			return services;
		}

		/// <summary>
		/// Transient lifetime services are created each time they are requested. This lifetime works best for lightweight, stateless services.
		/// </summary>
		static void AddTransientServices(IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<ContextUserFactory>();

			services.Configure<EmailSenderOptions>(configuration);
			services.AddTransient<IEmailSender, EmailSender>();
		}

		/// <summary>
		/// Scoped lifetime services are created once per request.
		/// </summary>
		static void AddScopedServices(IServiceCollection services, IConfiguration configuration) {
			services.AddScoped<AccountService>();
			services.AddScoped<BoardService>();
			services.AddScoped<MessageService>();
			services.AddScoped<NotificationService>();
			services.AddScoped<ProfileService>();
			services.AddScoped<RoleService>();
			services.AddScoped<SiteSettingsService>();
			services.AddScoped<SmileyService>();
			services.AddScoped<TopicService>();

			services.AddScoped<SiteSettingsRepository>();

			services.AddScoped((serviceProvider) => {
				var storageConnectionString = configuration[Constants.Keys.StorageConnection];

				if (string.IsNullOrEmpty(storageConnectionString))
					storageConnectionString = configuration.GetConnectionString(Constants.Keys.StorageConnection);

				var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

				return storageAccount.CreateCloudBlobClient();
			});
		}

		/// <summary>
		/// Singleton lifetime services are created the first time they are requested (or when ConfigureServices is run if you specify an instance there) and then every subsequent request will use the same instance.
		/// </summary>
		static void AddSingletonServices(IServiceCollection services, IConfiguration configuration) {
			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
		}
	}
}