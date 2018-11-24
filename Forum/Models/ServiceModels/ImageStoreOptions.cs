using System.IO;

namespace Forum.Models.ServiceModels {
	public class ImageStoreOptions {
		public string ContainerName { get; set; }
		public Stream InputStream { get; set; }
		public string FileName { get; set; }
		public int MaxDimension { get; set; }
		public bool Overwrite { get; set; }
	}
}