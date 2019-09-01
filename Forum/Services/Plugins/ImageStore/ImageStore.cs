using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Forum.Services.Plugins.ImageStore {
	public class ImageStore : IImageStore {
		CloudBlobClient CloudBlobClient { get; }

		public ImageStore(CloudBlobClient cloudBlobClient) => CloudBlobClient = cloudBlobClient;

		public async Task<string> Save(ImageStoreSaveOptions options) {
			var container = CloudBlobClient.GetContainerReference(options.ContainerName);

			if (await container.CreateIfNotExistsAsync()) {
				await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
			}

			var blobReference = container.GetBlockBlobReference(options.FileName);
			var exists = await blobReference.ExistsAsync();
			var expired = false;

			if (exists) {
				var lastModified = blobReference.Properties.LastModified ?? DateTime.Now;
				expired = lastModified < DateTime.Now.AddDays(-14);
			}

			if (!exists || options.Overwrite || expired) {
				blobReference.Properties.ContentType = options.ContentType;

				if (options.MaxDimension > 0) {
					await StoreResizedImage(options, blobReference);
				}
				else {
					options.InputStream.Position = 0;
					await blobReference.UploadFromStreamAsync(options.InputStream);
				}
			}

			return blobReference.Uri.AbsoluteUri;
		}

		public async Task Delete(ImageStoreDeleteOptions options) {
			var container = CloudBlobClient.GetContainerReference(options.ContainerName);

			if (await container.ExistsAsync()) {
				var blobReference = container.GetBlockBlobReference(options.Path);
				await blobReference.DeleteIfExistsAsync();
			}
		}

		async Task StoreResizedImage(ImageStoreSaveOptions options, CloudBlockBlob blobReference) {
			using var src = Image.FromStream(options.InputStream);
			var largestDimension = src.Width > src.Height ? src.Width : src.Height;
			var ratio = 1D * options.MaxDimension / largestDimension;
			var destinationWidth = Convert.ToInt32(src.Width * ratio);
			var destinationHeight = Convert.ToInt32(src.Height * ratio);

			using var targetImage = new Bitmap(destinationWidth, destinationHeight);

			using var graphics = Graphics.FromImage(targetImage);
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.DrawImage(src, 0, 0, targetImage.Width, targetImage.Height);

			using var memoryStream = new MemoryStream();
			targetImage.Save(memoryStream, ImageFormat.Png);
			memoryStream.Position = 0;

			await blobReference.UploadFromStreamAsync(memoryStream);
		}
	}
}