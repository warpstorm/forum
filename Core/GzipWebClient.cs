using Forum.Core.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Forum.Core {
	public class GzipWebClient : WebClient {
		public async Task<HtmlDocument> DownloadDocument(string remoteUrl) {
			var data = await DownloadStringSafe(remoteUrl);

			HtmlDocument returnObject = null;

			if (!string.IsNullOrEmpty(data)) {
				returnObject = new HtmlDocument();
				returnObject.LoadHtml(data);
			}

			return returnObject;
		}

		public async Task<T> DownloadJSObject<T>(string remoteUrl, JsonConverter jsonConverter = null) {
			var data = await DownloadStringSafe(remoteUrl);

			var returnObject = default(T);

			if (!string.IsNullOrEmpty(data)) {
				try {
					if (jsonConverter is null) {
						returnObject = JsonConvert.DeserializeObject<T>(data);
					}
					else {
						returnObject = JsonConvert.DeserializeObject<T>(data, jsonConverter);
					}
				}
				catch (JsonSerializationException) { }
				catch (JsonReaderException) { }
			}

			return returnObject;
		}

		public async Task<string> DownloadStringSafe(string remoteUrl) {
			remoteUrl = CleanUrl(remoteUrl);

			var data = string.Empty;

			try {
				data = await DownloadStringTaskAsync(remoteUrl);
			}
			catch (UriFormatException) { }
			catch (AggregateException) { }
			catch (ArgumentException) { }
			catch (WebException) { }

			return data;
		}

		protected override WebRequest GetWebRequest(Uri remoteUri) {
			ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			var request = base.GetWebRequest(remoteUri) as HttpWebRequest;

			request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.246";
			request.AllowAutoRedirect = true;
			request.MaximumAutomaticRedirections = 3;
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Timeout = 5000;
			request.CookieContainer = new CookieContainer();

			return request;
		}

		string CleanUrl(string remoteUrl) {
			remoteUrl.ThrowIfNull(nameof(remoteUrl));
			return remoteUrl.Split('#')[0];
		}
	}
}
