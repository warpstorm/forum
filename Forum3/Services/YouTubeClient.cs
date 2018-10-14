using System.Text.RegularExpressions;

namespace Forum3.Services {
	using ServiceModels = Models.ServiceModels;

	public class YouTubeClient {
		public bool TryGetReplacement(string remoteUrl, string pageTitle, string favicon, out ServiceModels.RemoteUrlReplacement replacement) {
			var regexVideo = new Regex(@"(?:https?:\/\/)?(?:www\.)?(?:(?:(?:youtube.com\/watch\?[^?]*v=|youtu.be\/)([\w\-]+))(?:[^\s?]+)?)", RegexOptions.Compiled | RegexOptions.Multiline);

			var matches = regexVideo.Match(remoteUrl);

			if (matches.Success) {
				var videoId = matches.Groups[1].Value;
				var videoElement = $"<iframe type='text/html' title='YouTube video player' class='youtubePlayer' src='https://www.youtube.com/embed/{videoId}?rel=0' frameborder='0' allowfullscreen='1'></iframe>";

				replacement = new ServiceModels.RemoteUrlReplacement {
					ReplacementText = $"<a target='_blank' href='{remoteUrl}'>{favicon}{pageTitle}</a>",
					Card = $"<div class='embedded-video'>{videoElement}</div>"
				};

				return true;
			}

			replacement = null;
			return false;
		}
	}
}
