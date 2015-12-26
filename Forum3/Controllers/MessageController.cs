using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Forum3.DataModels;
using System;
using Forum3.ViewModels.Message;
using Microsoft.AspNet.Authorization;
using Forum3.Data;
using System.Collections.Generic;
using CodeKicker.BBCode;
using System.Text.RegularExpressions;
using System.Net;
using HtmlAgilityPack;

namespace Forum3.Controllers {
	[Authorize]
	public class MessageController : Controller {
		private ApplicationDbContext _context;

		const string youtubePattern = @"(?:https?:\/\/)?(?:www\.)?(?:(?:(?:youtube.com\/watch\?[^?]*v=|youtu.be\/)([\w\-]+))(?:[^\s?]+)?)";
		const string youtubeIframePartial = "<iframe type='text/html' title='YouTube video player' class='youtubePlayer' src='http://www.youtube.com/embed/{0}' frameborder='0' allowfullscreen='1'></iframe>";
		const string gifvPartial = "<video autoplay loop><source src='{0}.webm' type='video/webm' /></video>";

		Regex regexUrl = new Regex("(^| )((https?\\://){1}\\S+)", RegexOptions.Compiled | RegexOptions.Multiline);
		Regex regexGifv = new Regex("(^| )((https?\\://){1}\\S+)\\.gifv", RegexOptions.Compiled | RegexOptions.Multiline);
		Regex regexYoutube = new Regex(youtubePattern);

		public MessageController(ApplicationDbContext context) {
			_context = context;
		}

		// GET: Message
		[AllowAnonymous]
		public async Task<IActionResult> Index() {
			var skip = 0;

			if (HttpContext.Request.Query.ContainsKey("skip"))
				skip = Convert.ToInt32(HttpContext.Request.Query["skip"]);

			var take = 15;

			if (HttpContext.Request.Query.ContainsKey("take"))
				take = Convert.ToInt32(HttpContext.Request.Query["take"]);

			var messageRecords = _context.Messages.Where(m => m.ParentId == 0).OrderByDescending(m => m.LastReplyPosted);

			var topicList = await messageRecords.Select(m => new Topic {
				Id = m.Id,
				Subject = m.ShortPreview,
				LastReplyId = m.LastReplyId,
				LastReplyById = m.LastReplyById,
				LastReplyPostedDT = m.LastReplyPosted,
				Views = m.Views,
				Replies = m.Replies,
			}).ToListAsync();

			var skipped = 0;
			var viewModel = new TopicIndex {
				Skip = skip + take,
				Take = take
			};

			foreach (var topic in topicList) {
				if (viewModel.Topics.Count() > take) {
					viewModel.MoreMessages = true;
					break;
				}

				if (skipped < skip) {
					skipped++;
					continue;
				}

				viewModel.Topics.Add(topic);
			}

			return View(viewModel);
		}

		// GET: Message/Create
		public IActionResult Create() {
			return View();
		}

		// POST: Message/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Input input) {
			if (ModelState.IsValid) {
				var processMessageBodyTask = ProcessMessageBody(input.Body);

				var processedMessageBody = await processMessageBodyTask;

				var newRecord = new Message {
					OriginalBody = processedMessageBody.OriginalBody,
					DisplayBody = processedMessageBody.DisplayBody,
				};

				_context.Messages.Add(newRecord);

				await _context.SaveChangesAsync();

				return RedirectToAction("Index");
			}

			return View(input);
		}

		// GET: Message/Edit/5
		public async Task<IActionResult> Edit(int? id) {
			if (id == null)
				return HttpNotFound();

			var message = await _context.Messages.SingleAsync(m => m.Id == id);

			if (message == null)
				return HttpNotFound();

			return View(message);
		}

		// POST: Message/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Message message) {
			if (ModelState.IsValid) {
				_context.Update(message);

				await _context.SaveChangesAsync();
				return RedirectToAction("Index");
			}

			return View(message);
		}
		
		// GET: Message/Delete/5
		public async Task<IActionResult> Delete(int id) {
			var message = await _context.Messages.SingleAsync(m => m.Id == id);

			_context.Messages.Remove(message);

			await _context.SaveChangesAsync();
			return RedirectToAction("Index");
		}

		private async Task<ProcessedMessageBody> ProcessMessageBody(string body) {
			body = body.Trim();

			if (string.IsNullOrEmpty(body))
				throw new Exception("Message body cannot be empty.");

			body = body.Replace("<3", "*heartsmiley*");

			var returnObject = new ProcessedMessageBody {
				OriginalBody = body,
				DisplayBody = body,
				MentionedUsers = new List<int>()
			};

			returnObject.DisplayBody = CustomTagParser(returnObject.DisplayBody);

			// make absolutely sure it targets a new window.
			returnObject.DisplayBody = new Regex(@"<a ").Replace(returnObject.DisplayBody, "<a target='_blank' ");

			// trim extra lines from quotes
			returnObject.DisplayBody = new Regex(@"<blockquote>\r*\n*").Replace(returnObject.DisplayBody, "<blockquote>");
			returnObject.DisplayBody = new Regex(@"\r*\n*</blockquote>\r*\n*").Replace(returnObject.DisplayBody, "</blockquote>");

			// keep this as close to the smiley replacement as possible to prevent HTML-izing the bracket.
			returnObject.DisplayBody = returnObject.DisplayBody.Replace("*heartsmiley*", "<3");

			// TODO - implement smileys here

			foreach (Match regexMatch in regexUrl.Matches(returnObject.DisplayBody)) {
				var siteUrl = regexMatch.Groups[2].Value;

				if (!string.IsNullOrEmpty(siteUrl)) {
					var remoteUrlReplacement = GetRemoteUrlReplacement(siteUrl);

					// Run the replacement on the displaybody
					remoteUrlReplacement.Regex.Replace(returnObject.DisplayBody, remoteUrlReplacement.ReplacementText, 1);
					returnObject.DisplayBody += remoteUrlReplacement.FollowOnText;
				}
			}

			returnObject.ShortPreview = GetShortPreview(returnObject.DisplayBody);

			return returnObject;
		}

		private string GetShortPreview(string messageBody) {
			var preview = Regex.Replace(messageBody, @"(<blockquote.*?>.+?</blockquote>\n*?)", string.Empty, RegexOptions.Compiled);

			// strip out tags
			preview = Regex.Replace(preview, @"(<.+?>|\[.+?\])", string.Empty, RegexOptions.Compiled);

			var matches = Regex.Match(preview, @"^(.{1,100})", RegexOptions.Compiled);

			preview = Regex.Match(matches.Groups[1].Value == "" ? preview : matches.Groups[1].Value, @"^(.+)?\n*", RegexOptions.Compiled).Groups[1].Value;

			if (preview.Length > 97) {
				matches = Regex.Match(preview, @"^(.{97})", RegexOptions.Compiled);
				return matches.Groups[1].Value + "...";
			}
			else if (preview.Length <= 0)
				return "No text";

			return preview;
		}

		private string GetLongPreview(string messageBody) {
			var preview = Regex.Replace(messageBody, @"(<blockquote.*?>.+?</blockquote>\n*?)", string.Empty, RegexOptions.Compiled);

			// strip out tags
			preview = Regex.Replace(preview, @"(<.+?>|\[.+?\])", string.Empty, RegexOptions.Compiled);

			var matches = Regex.Match(preview, @"^(.{1,500})", RegexOptions.Compiled);

			if (preview.Length > 500) {
				matches = Regex.Match(preview, @"^(.{97})", RegexOptions.Compiled);
				return matches.Groups[1].Value + "\n\n[more...]";
			}
			else if (preview.Length <= 0)
				return "No text";

			return preview;
		}

		private RemoteUrlReplacement GetRemoteUrlReplacement(string remoteUrl) {
			var remotePageDetails = GetRemotePageDetails(remoteUrl);

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

		private RemotePageDetails GetRemotePageDetails(string remoteUrl) {
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

		private string CustomTagParser(string body) {
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
					new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
				});

			return parser.ToHtml(body);
		}

		private class RemotePageDetails {
			public string Title { get; set; }
			public string Card { get; set; }
		}

		private class RemoteUrlReplacement {
			public Regex Regex { get; set; }
			public string ReplacementText { get; set; }
			public string FollowOnText { get; set; }
		}

		private class ProcessedMessageBody {
			public string OriginalBody { get; set; }
			public string DisplayBody { get; set; }
			public string ShortPreview { get; set; }
			public string LongPreview { get; set; }
			public string Boards { get; set; }
			public List<int> MentionedUsers { get; set; }
		}
	}
}
