namespace Forum3.Interfaces.Services {
	using ServiceModels = Forum3.Models.ServiceModels;

	public interface IUrlReplacementClient {
		bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out ServiceModels.RemoteUrlReplacement replacement);
	}
}
