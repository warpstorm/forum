namespace Forum.Contracts {
	public interface IUrlReplacementClient {
		bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out IUrlReplacement replacement);
	}
}
