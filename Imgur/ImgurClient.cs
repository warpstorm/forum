using Forum.Contracts;
using Forum.Core;
using Forum.ExternalClients.Imgur.Models;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Forum.ExternalClients.Imgur {
	public class ImgurClient : IUrlReplacementClient {
		const string endpoint = "https://api.imgur.com/3";

		GzipWebClient WebClient { get; }

		Dictionary<HttpRequestHeader, string> Headers { get; }

		public ImgurClient(
			GzipWebClient webClient,
			IOptions<ImgurClientOptions> optionsAccessor
		) {
			WebClient = webClient;

			Headers = new Dictionary<HttpRequestHeader, string> {
				{ HttpRequestHeader.Authorization, $"Client-ID {optionsAccessor.Value.ClientId}" }
			};
		}

		public bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out IUrlReplacement replacement) {
			replacement = null;

			var isReaction = remoteUrl.Contains("?forum-reaction");

			if (isReaction) {
				remoteUrl = Regex.Replace(remoteUrl, @"\?forum-reaction", string.Empty);
			}

			var albumMatch = Regex.Match(remoteUrl, @"imgur.com\/a\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);
			var galleryMatch = Regex.Match(remoteUrl, @"imgur.com\/gallery\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);
			var imageMatch = Regex.Match(remoteUrl, @"imgur.com\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);
			var favoriteMatch = Regex.Match(remoteUrl, @"imgur.com\/.+favorites.+\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);

			if (albumMatch.Success) {
				var hash = albumMatch.Groups[1].Value;
				replacement = GetReplacementForAlbum(hash, favicon);
			}
			else if (galleryMatch.Success) {
				var topLevelGalleries = new List<string> { "hot", "top", "user" };
				var hash = galleryMatch.Groups[1].Value;

				// We can't process top level galleries yet.
				if (!topLevelGalleries.Contains(hash)) {
					replacement = GetReplacementForGalleryAlbum(hash, favicon);
				}
			}
			else if (imageMatch.Success) {
				var hash = imageMatch.Groups[1].Value;
				replacement = GetReplacementForImage(hash, favicon, isReaction);
			}
			else if (favoriteMatch.Success) {
				var hash = favoriteMatch.Groups[1].Value;
				replacement = GetReplacementForImage(hash, favicon, isReaction);
			}

			if (replacement is null) {
				return false;
			}

			return true;
		}

		public IUrlReplacement GetReplacementForImage(string hash, string favicon, bool isReaction) {
			var image = GetImage(hash);

			if (image is null) {
				return null;
			}

			if (string.IsNullOrEmpty(image.Title)) {
				image.Title = "(No Title)";
			}

			if (isReaction) {
				string imageMarkup;

				if (!string.IsNullOrEmpty(image.Mp4)) {
					imageMarkup = $@"<video autoplay loop controls><source src='{image.Mp4}' type='video/mp4' /></video>";
				}
				else {
					imageMarkup = $@"<img src='{image.Link}' />";
				}

				return new ImgurUrlReplacement {
					ReplacementText = $"<div class='forum-reaction'>{imageMarkup}</div>"
				};
			}
			else {
				return new ImgurUrlReplacement {
					ReplacementText = $"<a target='_blank' href='{image.Link}'>{favicon}{image.Title}</a>",
					Card = GetCardForImages(new List<Image> { image })
				};
			}
		}

		public IUrlReplacement GetReplacementForAlbum(string hash, string favicon) {
			var album = GetAlbum(hash);

			if (album is null) {
				return null;
			}

			if (string.IsNullOrEmpty(album.Title)) {
				album.Title = "(No Title)";
			}

			return new ImgurUrlReplacement {
				ReplacementText = $"<a target='_blank' href='{album.Link}'>{favicon}{album.Title}</a>",
				Card = GetCardForImages(album.Images)
			};
		}

		public IUrlReplacement GetReplacementForGalleryAlbum(string hash, string favicon) {
			var album = GetGalleryAlbum(hash);

			if (album is null) {
				return null;
			}

			if (string.IsNullOrEmpty(album.Title)) {
				album.Title = "(No Title)";
			}

			return new ImgurUrlReplacement {
				ReplacementText = $"<a target='_blank' href='{album.Link}'>{favicon}{album.Title}</a>",
				Card = GetCardForImages(album.Images)
			};
		}

		public Image GetImage(string hash) {
			var requestUrl = $"{endpoint}/image/{hash}";
			var response = WebClient.DownloadJSObject<Response<Image>>(requestUrl, Headers);
			return response?.Data;
		}

		public Album GetAlbum(string hash) {
			var requestUrl = $"{endpoint}/album/{hash}";
			var response = WebClient.DownloadJSObject<Response<Album>>(requestUrl, Headers);
			return response?.Data;
		}

		public GalleryAlbum GetGalleryAlbum(string hash) {
			var requestUrl = $"{endpoint}/gallery/album/{hash}";
			var response = WebClient.DownloadJSObject<Response<GalleryAlbum>>(requestUrl, Headers);
			return response?.Data;
		}

		public List<Image> GetAlbumImages(string hash) {
			var requestUrl = $"{endpoint}/album/{hash}/images";
			var response = WebClient.DownloadJSObject<Response<List<Image>>>(requestUrl, Headers);
			return response.Data;
		}

		public string GetCardForImages(List<Image> images) {
			var card = string.Empty;

			foreach (var image in images) {
				var imageElement = string.Empty;

				if (!string.IsNullOrEmpty(image.Mp4)) {
					card += $@"<div class='embedded-video'>
								<p><video autoplay loop controls><source src='{image.Mp4}' type='video/mp4' /></video></p>
								<p>{image.Description}</p>
							</div>";
				}
				else {
					card += $@"<div class='imgur-image'>
								<p><a target='_blank' href='{image.Link}'><img src='{image.Link}' /></a></p>
								<p>{image.Description}</p>
							</div>";
				}
			}

			return card;
		}

		public bool CheckUserName(string username) {
			var requestUrl = $"{endpoint}/account/{username}";
			var response = WebClient.DownloadJSObject<Response<Account>>(requestUrl, Headers);
			return response.Success;
		}
	}
}
