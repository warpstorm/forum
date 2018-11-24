namespace Forum.Interfaces.Services {
	using ServiceModels = Forum.Models.ServiceModels;

	public interface IUrlReplacementClient {
		bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out ServiceModels.RemoteUrlReplacement replacement);
	}
}
