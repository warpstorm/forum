using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Interfaces.Filters;
using Forum3.Interfaces.Services;
using Forum3.Middleware;
using Forum3.Models.ServiceModels;
using Forum3.Services;
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
			RegisterRepositories(services, configuration);

			RegisterAzureStorage(services, configuration);

			services.Configure<RecaptchaOptions>(configuration);
			services.AddTransient<IRecaptchaValidator, RecaptchaValidator>();
			services.AddTransient<ValidateRecaptchaActionFilter>();

			services.Configure<EmailSenderOptions>(configuration);
			services.AddTransient<IEmailSender, EmailSender>();

			services.AddTransient<IImageStore, ImageStore>();
			services.AddTransient<IForumViewResult, ForumViewResult>();

			services.AddScoped<UserContext>();

			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();

			return services;
		}

		static void RegisterRepositories(IServiceCollection services, IConfiguration configuration) {
			services.AddScoped<Repositories.AccountRepository>();
			services.AddScoped<Repositories.BoardRepository>();
			services.AddScoped<Repositories.MessageRepository>();
			services.AddScoped<Repositories.NotificationRepository>();
			services.AddScoped<Repositories.RoleRepository>();
			services.AddScoped<Repositories.SettingsRepository>();
			services.AddScoped<Repositories.SmileyRepository>();
			services.AddScoped<Repositories.TopicRepository>();
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