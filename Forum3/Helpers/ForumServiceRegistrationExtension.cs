using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Forum3.Interfaces.Users;
using Forum3.Models.ServiceModels;
using Forum3.Services;
using Forum3.Services.Controller;

namespace Forum3.Helpers {
	public static class ForumServiceRegistrationExtension {
		public static IServiceCollection AddForum(this IServiceCollection services, IConfiguration configuration) {
			services.AddScoped<AccountService>();
			services.AddScoped<BoardService>();
			services.AddScoped<MessageService>();
			services.AddScoped<NotificationService>();
			services.AddScoped<ProfileService>();
			services.AddScoped<RoleService>();
			services.AddScoped<SiteSettingsService>();
			services.AddScoped<SmileyService>();
			services.AddScoped<TopicService>();

			services.AddTransient<ContextUserFactory>();

			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();

			services.Configure<EmailSenderOptions>(configuration);
			services.AddTransient<IEmailSender, EmailSender>();

			services.AddScoped((serviceProvider) => {
				var storageConnectionString = configuration["StorageConnection"];

				if (string.IsNullOrEmpty(storageConnectionString))
					storageConnectionString = configuration.GetConnectionString("StorageConnection");

				var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

				return storageAccount.CreateCloudBlobClient();
			});

			return services;
		}
	}
}