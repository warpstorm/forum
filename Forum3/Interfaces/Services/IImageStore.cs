using System.Threading.Tasks;

namespace Forum3.Interfaces.Services {
	using ServiceModels = Forum3.Models.ServiceModels;

	public interface IImageStore {
		Task<string> StoreImage(ServiceModels.ImageStoreOptions options);
	}
}