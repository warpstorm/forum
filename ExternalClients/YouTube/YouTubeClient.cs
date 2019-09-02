using Forum.Contracts;
using System.Text.RegularExpressions;

namespace Forum.ExternalClients.YouTube {
	public class YouTubeClient : IUrlReplacementClient {
		public bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out IUrlReplacement replacement) {
			replacement = null;

			var match = Regex.Match(remoteUrl, @"(?:https?:\/\/)?(?:www\.)?(?:(?:(?:youtube.com\/watch\?[^?]*v=|youtu.be\/)([\w\-]+))(?:[^\s?]+)?)", RegexOptions.Compiled | RegexOptions.Multiline);

			if (match.Success) {
				var videoId = match.Groups[1].Value;
				var videoElement = $"<iframe type='text/html' title='YouTube video player' class='youtubePlayer' src='https://www.youtube.com/embed/{videoId}?rel=0' frameborder='0' allowfullscreen='1'></iframe>";

				replacement = new YouTubeUrlReplacement {
					ReplacementText = $"<a target='_blank' href='{remoteUrl}'>{favicon}{pageTitle}</a>",
					Card = $"<div class='embedded-video'>{videoElement}</div>"
				};
			}

			return !(replacement is null);
		}
	}
}
