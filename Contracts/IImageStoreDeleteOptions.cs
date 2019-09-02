namespace Forum.Contracts {
	public interface IImageStoreDeleteOptions {
		string ContainerName { get; set; }
		string Path { get; set; }
	}
}