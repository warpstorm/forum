using Forum.Contracts;
using Forum.Core;
using Forum.Data.Contexts;
using Forum.ExternalClients.Imgur.JsonConverters;
using Forum.ExternalClients.Imgur.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum.ExternalClients.Imgur {
	public class ImgurClient {
		const string endpoint = "https://api.imgur.com/3";

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		GzipWebClient WebClient { get; }
		string ClientId { get; }
		string ClientSecret { get; }

		public ImgurClient(
			ApplicationDbContext dbContext,
			UserContext userContext,
			GzipWebClient webClient,
			IOptions<ImgurClientOptions> optionsAccessor
		) {
			DbContext = dbContext;
			UserContext = userContext;
			WebClient = webClient;

			ClientId = optionsAccessor.Value.ClientId;
			ClientSecret = optionsAccessor.Value.ClientSecret;
		}

		public async Task<IUrlReplacement> GetReplacement(string remoteUrl, string favicon) {
			var isReaction = remoteUrl.Contains("?forum-reaction");

			if (isReaction) {
				remoteUrl = Regex.Replace(remoteUrl, @"\?forum-reaction", string.Empty);
			}

			var albumMatch = Regex.Match(remoteUrl, @"imgur.com\/a\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);
			var galleryMatch = Regex.Match(remoteUrl, @"imgur.com\/gallery\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);
			var imageMatch = Regex.Match(remoteUrl, @"imgur.com\/([a-zA-Z0-9]+)?\.", RegexOptions.Compiled | RegexOptions.Multiline);
			var favoriteMatch = Regex.Match(remoteUrl, @"imgur.com\/.+favorites.+\/([a-zA-Z0-9]+)?$", RegexOptions.Compiled | RegexOptions.Multiline);

			IUrlReplacement replacement = null;

			if (albumMatch.Success) {
				var hash = albumMatch.Groups[1].Value;
				replacement = await GetReplacementForAlbum(hash, favicon);
			}
			else if (galleryMatch.Success) {
				var topLevelGalleries = new List<string> { "hot", "top", "user" };
				var hash = galleryMatch.Groups[1].Value;

				// We can't process top level galleries yet.
				if (!topLevelGalleries.Contains(hash)) {
					replacement = await GetReplacementForGalleryAlbum(hash, favicon);
				}
			}
			else if (imageMatch.Success) {
				var hash = imageMatch.Groups[1].Value;
				replacement = await GetReplacementForImage(hash, favicon, isReaction);
			}
			else if (favoriteMatch.Success) {
				var hash = favoriteMatch.Groups[1].Value;
				replacement = await GetReplacementForImage(hash, favicon, isReaction);
			}

			return replacement;
		}

		public async Task<IUrlReplacement> GetReplacementForImage(string hash, string favicon, bool isReaction) {
			var image = await GetImage(hash);

			if (image is null) {
				return null;
			}

			if (string.IsNullOrEmpty(image.Title)) {
				image.Title = "(No Title)";
			}

			if (isReaction) {
				string imageMarkup;

				if (!string.IsNullOrEmpty(image.Mp4)) {
					imageMarkup = $@"<video autoplay loop controls muted><source src='{image.Mp4}' type='video/mp4' /></video>";
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

		public async Task<IUrlReplacement> GetReplacementForAlbum(string hash, string favicon) {
			var album = await GetAlbum(hash);

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

		public async Task<IUrlReplacement> GetReplacementForGalleryAlbum(string hash, string favicon) {
			var album = await GetGalleryAlbum(hash);

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

		public async Task<Image> GetImage(string hash) {
			WebClient.Headers.Clear();
			WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Client-ID {ClientId}");

			var requestUrl = $"{endpoint}/image/{hash}";
			var response = await WebClient.DownloadJSObject<Response<Image>>(requestUrl);
			return response?.Data;
		}

		public async Task<Album> GetAlbum(string hash) {
			WebClient.Headers.Clear();
			WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Client-ID {ClientId}");

			var requestUrl = $"{endpoint}/album/{hash}";
			var response = await WebClient.DownloadJSObject<Response<Album>>(requestUrl);
			return response?.Data;
		}

		public async Task<GalleryAlbum> GetGalleryAlbum(string hash) {
			WebClient.Headers.Clear();
			WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Client-ID {ClientId}");

			var requestUrl = $"{endpoint}/gallery/album/{hash}";
			var response = await WebClient.DownloadJSObject<Response<GalleryAlbum>>(requestUrl);
			return response?.Data;
		}

		public async Task<List<Image>> GetAlbumImages(string hash) {
			WebClient.Headers.Clear();
			WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Client-ID {ClientId}");

			var requestUrl = $"{endpoint}/album/{hash}/images";
			var response = await WebClient.DownloadJSObject<Response<List<Image>>>(requestUrl);
			return response.Data;
		}

		public async Task<List<string>> GetFavorites() {
			WebClient.Headers.Clear();
			WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Client-ID {ClientId}");
			//WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {UserContext.Imgur.AccessToken}");

			var requestUrl = $"{endpoint}/account/{UserContext.Imgur.ImgurUserName}/favorites/";
			return await WebClient.DownloadJSObject<List<string>>(requestUrl, new FavoriteIdsConverter());
		}

		public async Task RefreshToken() {
			WebClient.Headers.Clear();
			WebClient.Headers.Add(HttpRequestHeader.Authorization, $"Client-ID {ClientId}");
			WebClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");

			var parameters = $@"refresh_token={UserContext.Imgur.RefreshToken}&client_id={ClientId}&client_secret={ClientSecret}&grant_type=refresh_token";
			var data = await WebClient.UploadStringTaskAsync("https://api.imgur.com/oauth2/token", parameters);

			RefreshToken refreshToken = null;

			if (!string.IsNullOrEmpty(data)) {
				try {
					refreshToken = JsonConvert.DeserializeObject<RefreshToken>(data);
				}
				catch (JsonSerializationException) { }
				catch (JsonReaderException) { }
			}

			if (!(refreshToken is null)) {
				UserContext.Imgur.AccessToken = refreshToken.AccessToken;
				UserContext.Imgur.AccessTokenExpiration = DateTime.Now.AddSeconds(refreshToken.ExpiresIn - 60);
				UserContext.Imgur.ImgurUserName = refreshToken.UserName;

				DbContext.Update(UserContext.Imgur);
				await DbContext.SaveChangesAsync();
			}
		}

		string GetCardForImages(List<Image> images) {
			var card = string.Empty;

			foreach (var image in images) {
				var imageElement = string.Empty;

				if (!string.IsNullOrEmpty(image.Mp4)) {
					card += $@"<div class='embedded-video'>
								<p><video loop controls muted><source src='{image.Mp4}' type='video/mp4' /></video></p>
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
