using Forum.Contracts;

namespace Forum.Models.ServiceModels {
	public class ImageStoreDeleteOptions : IImageStoreDeleteOptions {
		public string ContainerName { get; set; }
		public string Path { get; set; }
	}
}