namespace Forum.Plugins.UrlReplacement {
	public interface IUrlReplacementClient {
		bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out UrlReplacement replacement);
	}
}
