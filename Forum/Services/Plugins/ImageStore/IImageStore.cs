using System.Threading.Tasks;

namespace Forum.Services.Plugins.ImageStore {
	public interface IImageStore {
		Task<string> Save(ImageStoreSaveOptions options);
		Task Delete(ImageStoreDeleteOptions options);
	}
}