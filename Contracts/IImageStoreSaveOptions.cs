using System.IO;

namespace Forum.Contracts {
	public interface IImageStoreSaveOptions {
		string ContainerName { get; set; }
		string ContentType { get; set; }
		string FileName { get; set; }
		Stream InputStream { get; set; }
		int MaxDimension { get; set; }
		bool Overwrite { get; set; }
	}
}