using Forum3.Contexts;
using Forum3.Interfaces.Users;
using Forum3.Middleware;
using Forum3.Models.ServiceModels;
using Forum3.Processes.Topics;
using Forum3.Services;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using System;

// REMINDER -
// Transient: created each time they are requested. This lifetime works best for lightweight, stateless services.
// Scoped: created once per request.
// Singleton: created the first time they are requested (or when ConfigureServices is run if you specify an instance there) and then every subsequent request will use the same instance.

namespace Forum3.Extensions {
	public static class ForumStartupExtensions {
		public static IApplicationBuilder UseForum(this IApplicationBuilder builder) {
			builder.UseMiddleware<HttpStatusCodeHandler>();

			return builder;
		}

		public static IServiceCollection AddForum(this IServiceCollection services, IConfiguration configuration) {
			RegisterTopicServices(services, configuration);

			RegisterAzureStorage(services, configuration);
			RegisterControllerServices(services, configuration);

			services.Configure<EmailSenderOptions>(configuration);
			services.AddTransient<IEmailSender, EmailSender>();

			services.AddScoped<UserContext>();

			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();

			return services;
		}

		static void RegisterTopicServices(IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<ViewModelProviders.Topics.IndexPage>();
			services.AddTransient<ViewModelProviders.Topics.IndexMorePage>();
			services.AddTransient<ViewModelProviders.Topics.DisplayPage>();

			services.AddTransient<LatestTopic>();
			services.AddTransient<PinTopic>();
			services.AddTransient<RebuildThreadRelationships>();
			services.AddTransient<ToggleBoard>();
			services.AddTransient<LoadTopicPreview>();
			services.AddTransient<TopicUnreadLevelCalculator>();
		}

		static void RegisterControllerServices(IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<AccountService>();
			services.AddTransient<BoardService>();
			services.AddTransient<MessageService>();
			services.AddTransient<NotificationService>();
			services.AddTransient<ProfileService>();
			services.AddTransient<RoleService>();
			services.AddTransient<SiteSettingsService>();
			services.AddTransient<SmileyService>();
			services.AddTransient<SettingsRepository>();
		}

		static void RegisterAzureStorage(IServiceCollection services, IConfiguration configuration) {
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
	}
}