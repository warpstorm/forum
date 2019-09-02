using System.Threading.Tasks;

namespace Forum.Contracts {
	public interface IImageStore {
		Task<string> Save(IImageStoreSaveOptions options);
		Task Delete(IImageStoreDeleteOptions options);
	}
}