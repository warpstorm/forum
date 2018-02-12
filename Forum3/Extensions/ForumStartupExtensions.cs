using Forum3.Contexts;
using Forum3.Interfaces;
using Forum3.Interfaces.Users;
using Forum3.Middleware;
using Forum3.Models.ServiceModels;
using Forum3.Processes;
using Forum3.Services;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using System;

namespace Forum3.Extensions {
	public static class ForumStartupExtensions {
		public static IApplicationBuilder UseForum(this IApplicationBuilder builder) {
			builder.UseMiddleware<HttpStatusCodeHandler>();

			return builder;
		}

		public static IServiceCollection AddForum(this IServiceCollection services, IConfiguration configuration) {
			RegisterTransientDependencies(services, configuration);
			RegisterScopedDependencies(services, configuration);
			RegisterSingletonDependencies(services, configuration);

			RegisterControllerProcesses(services, configuration);

			return services;
		}

		static void RegisterControllerProcesses(IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<RebuildThreadRelationshipsProcess>();

			services.AddTransient<Func<Type, IControllerProcess>>((serviceProvider) => {
				return (processType) => serviceProvider.GetService(processType) as IControllerProcess;
			});
		}

		/// <summary>
		/// Transient lifetime services are created each time they are requested. This lifetime works best for lightweight, stateless services.
		/// </summary>
		static void RegisterTransientDependencies(IServiceCollection services, IConfiguration configuration) {
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
		static void RegisterScopedDependencies(IServiceCollection services, IConfiguration configuration) {
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
		static void RegisterSingletonDependencies(IServiceCollection services, IConfiguration configuration) {
			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
		}
	}
}