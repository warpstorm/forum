using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Forum.Services.Plugins.UrlReplacement {
	public class ImgurClient : IUrlReplacementClient {
		const string ENDPOINT = "https://api.imgur.com/3";

		GzipWebClient WebClient { get; }

		Dictionary<HttpRequestHeader, string> Headers { get; }

		public ImgurClient(
			GzipWebClient webClient,
			IOptions<ImgurClientModels.Options> optionsAccessor
		) {
			WebClient = webClient;

			Headers = new Dictionary<HttpRequestHeader, string> {
				{ HttpRequestHeader.Authorization, $"Client-ID {optionsAccessor.Value.ClientId}" }
			};
		}

		public bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out UrlReplacement replacement) {
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

		public UrlReplacement GetReplacementForImage(string hash, string favicon, bool isReaction) {
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

				return new UrlReplacement {
					ReplacementText = $"<div class='forum-reaction'>{imageMarkup}</div>"
				};
			}
			else {
				return new UrlReplacement {
					ReplacementText = $"<a target='_blank' href='{image.Link}'>{favicon}{image.Title}</a>",
					Card = GetCardForImages(new List<ImgurClientModels.Image> { image })
				};
			}
		}

		public UrlReplacement GetReplacementForAlbum(string hash, string favicon) {
			var album = GetAlbum(hash);

			if (album is null) {
				return null;
			}

			if (string.IsNullOrEmpty(album.Title)) {
				album.Title = "(No Title)";
			}

			return new UrlReplacement {
				ReplacementText = $"<a target='_blank' href='{album.Link}'>{favicon}{album.Title}</a>",
				Card = GetCardForImages(album.Images)
			};
		}

		public UrlReplacement GetReplacementForGalleryAlbum(string hash, string favicon) {
			var album = GetGalleryAlbum(hash);

			if (album is null) {
				return null;
			}

			if (string.IsNullOrEmpty(album.Title)) {
				album.Title = "(No Title)";
			}

			return new UrlReplacement {
				ReplacementText = $"<a target='_blank' href='{album.Link}'>{favicon}{album.Title}</a>",
				Card = GetCardForImages(album.Images)
			};
		}

		public ImgurClientModels.Image GetImage(string hash) {
			var requestUrl = $"{ENDPOINT}/image/{hash}";
			var response = WebClient.DownloadJSObject<ImgurClientModels.ImageResponse>(requestUrl, Headers);
			return response?.Data;
		}

		public ImgurClientModels.Album GetAlbum(string hash) {
			var requestUrl = $"{ENDPOINT}/album/{hash}";
			var response = WebClient.DownloadJSObject<ImgurClientModels.AlbumResponse>(requestUrl, Headers);
			return response?.Data;
		}

		public ImgurClientModels.GalleryAlbum GetGalleryAlbum(string hash) {
			var requestUrl = $"{ENDPOINT}/gallery/album/{hash}";
			var response = WebClient.DownloadJSObject<ImgurClientModels.GalleryAlbumResponse>(requestUrl, Headers);
			return response?.Data;
		}

		public List<ImgurClientModels.Image> GetAlbumImages(string hash) {
			var requestUrl = $"{ENDPOINT}/album/{hash}/images";
			var response = WebClient.DownloadJSObject<ImgurClientModels.AlbumImagesResponse>(requestUrl, Headers);
			return response.Data;
		}

		public string GetCardForImages(List<ImgurClientModels.Image> images) {
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
	}
}
