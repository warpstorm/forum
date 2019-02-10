using System.IO;

namespace Forum.Services.Plugins.ImageStore {
	public class ImageStoreSaveOptions {
		public string ContainerName { get; set; }
		public Stream InputStream { get; set; }
		public string FileName { get; set; }
		public string ContentType { get; set; }
		public int MaxDimension { get; set; }
		public bool Overwrite { get; set; }
	}
}