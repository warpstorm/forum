using Forum3.Extensions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace Forum3.Services {
	public class GzipWebClient : WebClient {
		public HtmlDocument DownloadDocument(string remoteUrl) {
			var data = GetRemoteData(remoteUrl);

			try {
				data = DownloadString(remoteUrl);
			}
			catch (UriFormatException) { }
			catch (AggregateException) { }
			catch (ArgumentException) { }
			catch (WebException) { }

			HtmlDocument returnObject = null;

			if (!string.IsNullOrEmpty(data)) {
				returnObject = new HtmlDocument();
				returnObject.LoadHtml(data);
			}

			return returnObject;
		}

		public T DownloadJSObject<T>(string remoteUrl, Dictionary<HttpRequestHeader, string> headers = null) {
			remoteUrl = CleanUrl(remoteUrl);

			foreach (var header in headers)
				Headers.Set(header.Key, header.Value);

			var returnObject = default(T);
			var data = string.Empty;

			try {
				data = DownloadString(remoteUrl);
			}
			catch (UriFormatException) { }
			catch (AggregateException) { }
			catch (ArgumentException) { }
			catch (WebException) { }

			if (!string.IsNullOrEmpty(data)) {
				try {
					returnObject = JsonConvert.DeserializeObject<T>(data);
				}
				catch (JsonSerializationException) { }
				catch (JsonReaderException) { }
			}

			return returnObject;
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

		string GetRemoteData(string remoteUrl) {
			remoteUrl = CleanUrl(remoteUrl);

			var data = string.Empty;

			try {
				data = DownloadString(remoteUrl);
			}
			catch (UriFormatException) { }
			catch (AggregateException) { }
			catch (ArgumentException) { }
			catch (WebException) { }

			return data;
		}
	}
}
