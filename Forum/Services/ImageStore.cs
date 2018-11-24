using Forum.Contexts;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Forum.Services {
	using ServiceModels = Models.ServiceModels;

	public class ImageStore : IImageStore {
		UserContext UserContext { get; }
		SettingsRepository SettingsRepository { get; }
		CloudBlobClient CloudBlobClient { get; }

		public ImageStore(
			UserContext userContext,
			SettingsRepository settingsRepository,
			CloudBlobClient cloudBlobClient
		) {
			UserContext = userContext;
			SettingsRepository = settingsRepository;
			CloudBlobClient = cloudBlobClient;
		}

		public async Task<string> StoreImage(ServiceModels.ImageStoreOptions options) {
			var container = CloudBlobClient.GetContainerReference(options.ContainerName);

			if (await container.CreateIfNotExistsAsync())
				await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

			var blobReference = container.GetBlockBlobReference($"{options.FileName}.png");
			var exists = await blobReference.ExistsAsync();
			var expired = false;

			if (exists) {
				var lastModified = blobReference.Properties.LastModified ?? DateTime.Now;
				expired = lastModified < SettingsRepository.HistoryTimeLimit(true);
			}

			if (!exists || options.Overwrite || expired) {
				blobReference.Properties.ContentType = "image/png";

				using (var src = Image.FromStream(options.InputStream)) {
					var largestDimension = src.Width > src.Height ? src.Width : src.Height;

					var ratio = 1D * options.MaxDimension / largestDimension;

					var destinationWidth = Convert.ToInt32(src.Width * ratio);
					var destinationHeight = Convert.ToInt32(src.Height * ratio);

					using (var targetImage = new Bitmap(destinationWidth, destinationHeight)) {
						using (var g = Graphics.FromImage(targetImage)) {
							g.SmoothingMode = SmoothingMode.AntiAlias;
							g.InterpolationMode = InterpolationMode.HighQualityBicubic;
							g.DrawImage(src, 0, 0, targetImage.Width, targetImage.Height);
						}

						using (var memoryStream = new MemoryStream()) {
							targetImage.Save(memoryStream, ImageFormat.Png);
							memoryStream.Position = 0;

							await blobReference.UploadFromStreamAsync(memoryStream);
						}
					}
				}
			}

			return blobReference.Uri.AbsoluteUri;
		}
	}
}