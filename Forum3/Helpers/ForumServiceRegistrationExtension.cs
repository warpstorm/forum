using Forum3.Contexts;
using Forum3.Interfaces.Users;
using Forum3.Models.ServiceModels;
using Forum3.Services;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using System;

namespace Forum3.Helpers {
	public static class ForumServiceRegistrationExtension {
		public static IServiceCollection AddForum(this IServiceCollection services, IConfiguration configuration) {
			AddTransientDependencies(services, configuration);
			AddScopedDependencies(services, configuration);
			AddSingletonDependencies(services, configuration);

			return services;
		}

		/// <summary>
		/// Transient lifetime services are created each time they are requested. This lifetime works best for lightweight, stateless services.
		/// </summary>
		static void AddTransientDependencies(IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<AccountService>();
			services.AddTransient<BoardService>();
			services.AddTransient<MessageService>();
			services.AddTransient<NotificationService>();
			services.AddTransient<ProfileService>();
			services.AddTransient<RoleService>();
			services.AddTransient<SiteSettingsService>();
			services.AddTransient<SmileyService>();
			services.AddTransient<TopicService>();
			services.AddTransient<SettingsRepository>();

			services.Configure<EmailSenderOptions>(configuration);
			services.AddTransient<IEmailSender, EmailSender>();
		}

		/// <summary>
		/// Scoped lifetime services are created once per request.
		/// </summary>
		static void AddScopedDependencies(IServiceCollection services, IConfiguration configuration) {
			services.AddScoped<UserContext>();

			// Azure Storage
			services.AddScoped((serviceProvider) => {
				var storageConnectionString = configuration[Constants.Keys.StorageConnection];

				if (string.IsNullOrEmpty(storageConnectionString))
					storageConnectionString = configuration.GetConnectionString(Constants.Keys.StorageConnection);

				if (string.IsNullOrEmpty(storageConnectionString))
					throw new ApplicationException("No storage connection string found.");

				var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

				return storageAccount.CreateCloudBlobClient();
			});
		}

		/// <summary>
		/// Singleton lifetime services are created the first time they are requested (or when ConfigureServices is run if you specify an instance there) and then every subsequent request will use the same instance.
		/// </summary>
		static void AddSingletonDependencies(IServiceCollection services, IConfiguration configuration) {
			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
		}
	}
}