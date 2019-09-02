using Forum.Contracts;
using Forum.Core.Models.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;

namespace Forum.ExternalClients.AzureStorage {
	public static class AzureStorageClientStartupExtension {
		const string configKey = "StorageConnection";

		public static IServiceCollection AddAzureStorageClient(this IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<IImageStore, AzureStorageClient>();

			services.AddScoped((serviceProvider) => {
				// Try to pull from the environment first
				var storageConnectionString = configuration[configKey];

				if (string.IsNullOrEmpty(storageConnectionString)) {
					storageConnectionString = configuration.GetConnectionString(configKey);
				}

				if (string.IsNullOrEmpty(storageConnectionString)) {
					throw new HttpInternalServerError("No storage connection string found.");
				}

				var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

				return storageAccount.CreateCloudBlobClient();
			});

			return services;
		}
	}
}
