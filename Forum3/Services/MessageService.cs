using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HtmlAgilityPack;
using CodeKicker.BBCode;
using Forum3.DataModels;
using Forum3.Data;
using Forum3.ViewModels.Messages;

namespace Forum3.Services {
	public class MessageService {
		readonly ApplicationDbContext _dbContext;
		readonly IHttpContextAccessor _httpContextAccessor;
		readonly UserManager<ApplicationUser> _userManager;

		public MessageService(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager) {
			_dbContext = dbContext;
			_httpContextAccessor = httpContextAccessor;
			_userManager = userManager;
		}

		/// <summary>
		/// Coordinates the overall processing of the message input.
		/// </summary>
		public async Task CreateAsync(string messageBody, int parentId = 0, int replyId = 0) {
			if (replyId > 0) {
				var replyMessage = _dbContext.Messages.FirstOrDefault(m => m.Id == replyId);

				if (replyMessage == null)
					throw new Exception("Target message for reply doesn't exist.");

				parentId = replyMessage.ParentId;
			}

			var processedMessageInput = ProcessMessageInput(messageBody);

			var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

			var newRecord = new DataModels.Message {
				OriginalBody = processedMessageInput.OriginalBody,
				DisplayBody = processedMessageInput.DisplayBody,
				ShortPreview = processedMessageInput.ShortPreview,
				LongPreview = processedMessageInput.LongPreview,

				TimePosted = DateTime.Now,
				PostedById = currentUser.Id,
				PostedByName = currentUser.DisplayName,

				TimeEdited = DateTime.Now,
				EditedById = currentUser.Id,
				EditedByName = currentUser.DisplayName,

				ParentId = parentId,
				ReplyId = replyId,
			};

			_dbContext.Messages.Add(newRecord);

			await _dbContext.SaveChangesAsync();
		}

		public async Task UpdateAsync(int id, string messageBody) {
			var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

			var getRecordTask = _dbContext.Messages.SingleAsync(m => m.Id == id);

			var processedMessageInput = ProcessMessageInput(messageBody);

			var message = await getRecordTask;

			message.OriginalBody = processedMessageInput.OriginalBody;
			message.DisplayBody = processedMessageInput.DisplayBody;
			message.ShortPreview = processedMessageInput.ShortPreview;
			message.LongPreview = processedMessageInput.LongPreview;
			message.TimeEdited = DateTime.Now;

			message.EditedById = currentUser.Id;
			message.EditedByName = currentUser.DisplayName;

			_dbContext.Update(message);

			await _dbContext.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id) {
			var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

			var message = await _dbContext.Messages.SingleAsync(m => m.Id == id);
			var replies = await _dbContext.Messages.Where(m => m.ReplyId == id).ToListAsync();

			foreach (var reply in replies) {
				reply.OriginalBody =
					"[quote]" +
					message.OriginalBody +
					"\n" + 
					"Message deleted by " + currentUser.DisplayName +
					" on " + DateTime.Now.ToString("MMMM dd, yyyy") +
					"[/quote]" + 
					reply.OriginalBody;
				reply.ReplyId = 0;
				_dbContext.Entry(reply).State = EntityState.Modified;
			}

			_dbContext.Messages.Remove(message);

			await _dbContext.SaveChangesAsync();
		}

		public DataModels.Message Find(int id) {
			var message = _dbContext.Messages.SingleOrDefault(m => m.Id == id);

			if (message == null)
				throw new Exception("No message found with that ID.");

			return message;
		}

		ProcessedMessageInput ProcessMessageInput(string messageBody) {
			var processedMessageInput = PreProcessMessageInput(messageBody);

			ParseBBC(processedMessageInput);

			// TODO - implement smileys here

			ProcessMessageBodyUrls(processedMessageInput);
			FindMentionedUsers(processedMessageInput);
			PostProcessMessageInput(processedMessageInput);

			return processedMessageInput;
		}

		/// <summary>
		/// Some minor housekeeping on the message before we get into the heavy lifting.
		/// </summary>
		ProcessedMessageInput PreProcessMessageInput(string messageBody) {
			var processedMessageInput = new ProcessedMessageInput {
				OriginalBody = messageBody,
				DisplayBody = messageBody,
				MentionedUsers = new List<string>()
			};

			var displayBody = processedMessageInput.DisplayBody;

			displayBody = displayBody.Trim();

			if (string.IsNullOrEmpty(displayBody))
				throw new Exception("Message body cannot be empty.");

			// make absolutely sure it targets a new window.
			displayBody = new Regex(@"<a ").Replace(displayBody, "<a target='_blank' ");

			// trim extra lines from quotes
			displayBody = new Regex(@"<blockquote>\r*\n*").Replace(displayBody, "<blockquote>");
			displayBody = new Regex(@"\r*\n*</blockquote>\r*\n*").Replace(displayBody, "</blockquote>");

			// keep this as close to the smiley replacement as possible to prevent HTML-izing the bracket.
			displayBody = displayBody.Replace("*heartsmiley*", "<3");

			return processedMessageInput;
		}

		/// <summary>
		/// A drop in replacement for the default CodeKicker BBC parser that handles strike and heart smileys
		/// </summary>
		string ParseBBC(ProcessedMessageInput processedMessageInput) {
			var displayBody = processedMessageInput.DisplayBody;

			// eventually expand this to include all smileys that could have a gt/lt in it.
			displayBody = displayBody.Replace("<3", "*heartsmiley*");

			var parser = new BBCodeParser(new[] {
				new BBTag("b", "<span style=\"font-weight: bold;\">", "</span>"),
				new BBTag("s", "<span style=\"text-decoration: line-through;\">", "</span>"),
				new BBTag("i", "<span style=\"font-style: italic;\">", "</span>"),
				new BBTag("u", "<span style=\"text-decoration: underline;\">", "</span>"),
				new BBTag("code", "<pre>", "</pre>"),
				new BBTag("img", "<img src=\"${content}\" />", "", false, true),
				new BBTag("quote", "<blockquote>", "</blockquote>"),
				new BBTag("list", "<ul>", "</ul>"),
				new BBTag("*", "<li>", "</li>", true, false),
				new BBTag("url", "<a href=\"${href}\" target=\"_blank\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
			});

			return parser.ToHtml(displayBody);
		}

		/// <summary>
		/// Attempt to replace URLs in the message body with something better
		/// </summary>
		void ProcessMessageBodyUrls(ProcessedMessageInput processedMessageInput) {
			var displayBody = processedMessageInput.DisplayBody;

			var regexUrl = new Regex("(^| )((https?\\://){1}\\S+)", RegexOptions.Compiled | RegexOptions.Multiline);

			foreach (Match regexMatch in regexUrl.Matches(displayBody)) {
				var siteUrl = regexMatch.Groups[2].Value;

				if (!string.IsNullOrEmpty(siteUrl)) {
					var remoteUrlReplacement = GetRemoteUrlReplacement(siteUrl);

					// Run the replacement on the displaybody
					remoteUrlReplacement.Regex.Replace(displayBody, remoteUrlReplacement.ReplacementText, 1);

					displayBody += remoteUrlReplacement.FollowOnText;
				}
			}
		}

		/// <summary>
		/// Attempt to replace the ugly URL with a human readable title.
		/// </summary>
		RemoteUrlReplacement GetRemoteUrlReplacement(string remoteUrl) {
			var remotePageDetails = GetRemotePageDetails(remoteUrl);

			const string youtubePattern = @"(?:https?:\/\/)?(?:www\.)?(?:(?:(?:youtube.com\/watch\?[^?]*v=|youtu.be\/)([\w\-]+))(?:[^\s?]+)?)";
			const string youtubeIframePartial = "<iframe type='text/html' title='YouTube video player' class='youtubePlayer' src='http://www.youtube.com/embed/{0}' frameborder='0' allowfullscreen='1'></iframe>";
			const string gifvPartial = "<video autoplay loop><source src='{0}.webm' type='video/webm' /></video>";

			var regexYoutube = new Regex(youtubePattern);
			var regexGifv = new Regex("(^| )((https?\\://){1}\\S+)\\.gifv", RegexOptions.Compiled | RegexOptions.Multiline);
			var regexUrl = new Regex("(^| )((https?\\://){1}\\S+)", RegexOptions.Compiled | RegexOptions.Multiline);

			// check first if the link is a youtube vid
			if (regexYoutube.Match(remoteUrl).Success) {
				var youtubeVideoId = regexYoutube.Match(remoteUrl).Groups[1].Value;
				var youtubeIframeClosed = string.Format(youtubeIframePartial, youtubeVideoId);

				return new RemoteUrlReplacement {
					Regex = regexYoutube,
					ReplacementText = "<a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
					FollowOnText = " <br /><br />" + youtubeIframeClosed
				};
			}
			// or is it a gifv link
			else if (regexGifv.Match(remoteUrl).Success) {
				var gifvId = regexGifv.Match(remoteUrl).Groups[2].Value;
				var gifvEmbedded = string.Format(gifvPartial, gifvId);

				return new RemoteUrlReplacement {
					Regex = regexGifv,
					ReplacementText = " <a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
					FollowOnText = " <br /><br />" + gifvEmbedded
				};
			}

			// replace the URL with the HTML
			return new RemoteUrlReplacement {
				Regex = regexUrl,
				ReplacementText = "$1 <a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
				FollowOnText = " <br /><br />" + remotePageDetails.Card
			};
		}

		/// <summary>
		/// I really should make this async. Load a remote page by URL and attempt to get details about it.
		/// </summary>
		RemotePageDetails GetRemotePageDetails(string remoteUrl) {
			var returnResult = new RemotePageDetails {
				Title = remoteUrl
			};

			var siteWithoutHash = remoteUrl.Split('#')[0];

			HtmlDocument document = null;

			// An attempt to fix the random errors that Icy gets from posting links to Reddit
			ServicePointManager.Expect100Continue = false;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			var client = new HtmlWeb();

			client.PreRequest += request => {
				request.UserAgent = "MOZILLA/5.0 (WINDOWS NT 6.1; WOW64) APPLEWEBKIT/537.1 (KHTML, LIKE GECKO) CHROME/21.0.1180.75 SAFARI/537.1";
				request.CookieContainer = new CookieContainer();
				request.Timeout = 5000;
				request.AllowAutoRedirect = true;
				request.ProtocolVersion = HttpVersion.Version11;
				request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
				request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
				return true;
			};

			try {
				// If URL is malformed, this will fail
				document = client.Load(siteWithoutHash);
			}
			catch (Exception e) {
				returnResult.Card = e.Message;
				return returnResult;
			}

			// try to find the opengraph title
			var ogTitle = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:title']");
			var ogSiteName = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:site_name']");
			var ogImage = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:image']");
			var ogDescription = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:description']");

			if (ogTitle != null && ogTitle.Attributes["content"] != null && !string.IsNullOrEmpty(ogTitle.Attributes["content"].Value.Trim())) {
				returnResult.Title = ogTitle.Attributes["content"].Value.Trim();

				if (ogDescription != null && ogDescription.Attributes["content"] != null && !string.IsNullOrEmpty(ogDescription.Attributes["content"].Value.Trim())) {
					returnResult.Card += "<blockquote class='card'>";

					if (ogImage != null && ogImage.Attributes["content"] != null && !string.IsNullOrEmpty(ogImage.Attributes["content"].Value.Trim()))
						returnResult.Card += "<div class='cardImage'><img src='" + ogImage.Attributes["content"].Value.Trim() + "' /></div>";

					returnResult.Card += "<div>";
					returnResult.Card += "<p class='cardTitle'><a target='_blank' href='" + remoteUrl + "'>" + returnResult.Title + "</a></p>";

					var decodedDescription = WebUtility.HtmlDecode(ogDescription.Attributes["content"].Value.Trim());

					returnResult.Card += "<p class='cardDescription'>" + decodedDescription + "</p>";

					if (ogSiteName != null && ogSiteName.Attributes["content"] != null && !string.IsNullOrEmpty(ogSiteName.Attributes["content"].Value.Trim()))
						returnResult.Card += "<p class='cardLink'><a target='_blank' href='" + remoteUrl + "'>[" + ogSiteName.Attributes["content"].Value.Trim() + "]</a></p>";
					else
						returnResult.Card += "<p class='cardLink'><a target='_blank' href='" + remoteUrl + "'>[Direct Link]</a></p>";

					returnResult.Card += "</div><br class='clear' /></blockquote>";
				}
			}

			// if not, then try to find the title tag
			if (returnResult.Title == remoteUrl) {
				ogTitle = document.DocumentNode.SelectSingleNode(@"//title");

				if (ogTitle != null && !string.IsNullOrEmpty(ogTitle.InnerText.Trim()))
					returnResult.Title = ogTitle.InnerText.Trim();
			}

			return returnResult;
		}

		/// <summary>
		/// Searches a post for references to other users
		/// </summary>
		async void FindMentionedUsers(ProcessedMessageInput processedMessageInput) {
			var currentUser = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);

			var regexUsers = new Regex(@"@(\S+)");

			foreach (Match regexMatch in regexUsers.Matches(processedMessageInput.DisplayBody)) {
				var matchedTag = regexMatch.Groups[1].Value;

				var user = _dbContext.Users.Single(u => u.UserName.ToLower() == matchedTag.ToLower());

				// try to guess what they meant
				if (user == null)
					user = _dbContext.Users.Single(u => u.UserName.ToLower().Contains(matchedTag.ToLower()));

				if (user != null) {
					if (user.Id != currentUser.Id)
						processedMessageInput.MentionedUsers.Add(user.Id);

					// Eventually link to user profiles
					// returnObject.ProcessedBody = Regex.Replace(returnObject.ProcessedBody, @"@" + regexMatch.Groups[1].Value, "<a href='/Account/Details/" + user.UserId + "' class='user'>" + user.DisplayName + "</span>");
				}
			}
		}

		/// <summary>
		/// Minor post processing
		/// </summary>
		void PostProcessMessageInput(ProcessedMessageInput processedMessageInput) {
			processedMessageInput.DisplayBody = processedMessageInput.DisplayBody.Trim();
			processedMessageInput.ShortPreview = GetMessagePreview(processedMessageInput.DisplayBody, 100);
			processedMessageInput.LongPreview = GetMessagePreview(processedMessageInput.DisplayBody, 500, true);
		}

		/// <summary>
		/// Gets a reduced version of the message without HTML
		/// </summary>
		string GetMessagePreview(string messageBody, int previewLength, bool multiline = false) {
			// strip out quotes
			var preview = Regex.Replace(messageBody, @"(<blockquote.*?>.+?</blockquote>\n*?)", string.Empty, RegexOptions.Compiled);

			// strip out tags
			preview = Regex.Replace(preview, @"(<.+?>|\[.+?\])", string.Empty, RegexOptions.Compiled);

			var matches = Regex.Match(preview, @"^(.{1," + previewLength + "})", RegexOptions.Compiled);

			if (!multiline)
				preview = Regex.Match(matches.Groups[1].Value == "" ? preview : matches.Groups[1].Value, @"^(.+)?\n*", RegexOptions.Compiled).Groups[1].Value;

			if (preview.Length > previewLength) {
				matches = Regex.Match(preview, @"^(.{" + (previewLength - 3) + "})", RegexOptions.Compiled);
				return matches.Groups[1].Value + "...";
			}
			else if (preview.Length <= 0)
				return "No text";

			return preview;
		}

		class RemotePageDetails {
			public string Title { get; set; }
			public string Card { get; set; }
		}

		class RemoteUrlReplacement {
			public Regex Regex { get; set; }
			public string ReplacementText { get; set; }
			public string FollowOnText { get; set; }
		}
	}
}
