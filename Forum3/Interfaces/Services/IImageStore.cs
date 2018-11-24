using System.Threading.Tasks;

namespace Forum.Interfaces.Services {
	using ServiceModels = Forum.Models.ServiceModels;

	public interface IImageStore {
		Task<string> StoreImage(ServiceModels.ImageStoreOptions options);
	}
}