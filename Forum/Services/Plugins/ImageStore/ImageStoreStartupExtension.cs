using Forum.Models.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;

namespace Forum.Plugins.ImageStore {
	public static class ImageStoreStartupExtension {
		public static IServiceCollection AddImageStore(this IServiceCollection services, IConfiguration configuration) {
			services.AddTransient<IImageStore, ImageStore>();

			services.AddScoped((serviceProvider) => {
				// Try to pull from the environment first
				var storageConnectionString = configuration[Constants.InternalKeys.StorageConnection];

				if (string.IsNullOrEmpty(storageConnectionString)) {
					storageConnectionString = configuration.GetConnectionString(Constants.InternalKeys.StorageConnection);
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
