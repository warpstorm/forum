using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using CodeKicker.BBCode;
using HtmlAgilityPack;
using Forum3.Controllers;
using Forum3.Data;
using Forum3.Models.DataModels;
using Forum3.Models.InputModels;
using Forum3.Models.ServiceModels;
using Forum3.Models.ViewModels.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Services {
	public class MessageService {
		ApplicationDbContext DbContext { get; }
		UserService UserService { get; set; }
		IUrlHelperFactory UrlHelperFactory { get; set; }
		IActionContextAccessor ActionContextAccessor { get; set; }

		public MessageService(
			ApplicationDbContext dbContext,
			UserService userService,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserService = userService;
			ActionContextAccessor = actionContextAccessor;
			UrlHelperFactory = urlHelperFactory;
		}

		public async Task<CreateTopicPage> CreatePage(int boardId = 0) {
			var board = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == boardId);

			if (board == null)
				throw new Exception($"A record does not exist with ID '{boardId}'");

			var viewModel = new CreateTopicPage {
				BoardId = boardId
			};

			return viewModel;
		}

		public async Task<EditMessagePage> EditPage(int messageId) {
			var record = await DbContext.Messages.SingleOrDefaultAsync(m => m.Id == messageId);

			if (record == null)
				throw new Exception($"A record does not exist with ID '{messageId}'");

			var viewModel = new EditMessagePage {
				Id = messageId,
				Body = record.OriginalBody
			};

			return viewModel;
		}

		public async Task<ServiceResponse> CreateTopic(MessageInput input) {
			var serviceResponse = new ServiceResponse();

			var boardId = 0;

			if (input.BoardId != null) {
				boardId = Convert.ToInt32(input.BoardId);

				var board = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == boardId);

				if (board == null)
					serviceResponse.ModelErrors.Add(string.Empty, $"A record does not exist with ID '{boardId}'");
			}

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			var processedMessage = await ProcessMessageInput(serviceResponse, input.Body);

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);

			var record = await CreateMessageRecord(processedMessage, null);

			DbContext.MessageBoards.Add(new MessageBoard {
				MessageId = record.Id,
				BoardId = boardId,
				TimeAdded = DateTime.Now,
				UserId = UserService.ContextUser.ApplicationUser.Id
			});

			await DbContext.SaveChangesAsync();

			serviceResponse.RedirectPath = urlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.Id });

			return serviceResponse;
		}

		public async Task<ServiceResponse> CreateReply(MessageInput input) {
			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);

			var serviceResponse = new ServiceResponse();

			if (input.Id == 0)
				throw new Exception($"No record ID specified.");

			var processedMessageTask = ProcessMessageInput(serviceResponse, input.Body);
			var replyRecordTask = DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			await Task.WhenAll(replyRecordTask, processedMessageTask);

			var replyRecord = await replyRecordTask;
			var processedMessage = await processedMessageTask;

			if (replyRecord == null)
				serviceResponse.ModelErrors.Add(string.Empty, $"A record does not exist with ID '{input.Id}'");

			if (!serviceResponse.ModelErrors.Any()) {
				var record = await CreateMessageRecord(processedMessage, replyRecord);
				serviceResponse.RedirectPath = urlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.Id });
			}

			return serviceResponse;
		}

		public async Task<ServiceResponse> EditMessage(MessageInput input) {
			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);

			var serviceResponse = new ServiceResponse();

			if (input.Id == 0)
				throw new Exception($"No record ID specified.");

			var processedMessageTask = ProcessMessageInput(serviceResponse, input.Body);
			var recordTask = DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			await Task.WhenAll(recordTask, processedMessageTask);

			var record = await recordTask;
			var processedMessage = await processedMessageTask;

			if (!serviceResponse.ModelErrors.Any()) {
				await UpdateMessageRecord(processedMessage, record);
				serviceResponse.RedirectPath = urlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.Id });
			}

			return serviceResponse;
		}

		public async Task DeleteMessage(int messageId) {
			var record = await DbContext.Messages.SingleAsync(m => m.Id == messageId);
			
			if (record == null)
				throw new Exception($"A record does not exist with ID '{messageId}'");

			if (record.ParentId != 0) {
				var directReplies = await DbContext.Messages.Where(m => m.ReplyId == messageId).ToListAsync();

				foreach (var reply in directReplies) {
					reply.OriginalBody = 
						$"[quote]{record.OriginalBody}\n" +
						$"Message deleted by {UserService.ContextUser.ApplicationUser.DisplayName} on {DateTime.Now.ToString("MMMM dd, yyyy")}[/quote]" +
						reply.OriginalBody;

					reply.ReplyId = 0;

					DbContext.Entry(reply).State = EntityState.Modified;
				}
			}

			var topicReplies = await DbContext.Messages.Where(m => m.ParentId == messageId).ToListAsync();

			foreach (var reply in topicReplies)
				DbContext.Messages.Remove(reply);

			var messageBoards = await DbContext.MessageBoards.Where(m => m.MessageId == record.Id).ToListAsync();

			foreach (var messageBoard in messageBoards)
				DbContext.MessageBoards.Remove(messageBoard);

			DbContext.Messages.Remove(record);

			await DbContext.SaveChangesAsync();
		}

		async Task<ProcessedMessageInput> ProcessMessageInput(ServiceResponse serviceResponse, string messageBody) {
			var processedMessage = PreProcessMessageInput(messageBody);

			ParseBBC(processedMessage);

			// TODO - implement smileys here

			ProcessMessageBodyUrls(processedMessage);
			await FindMentionedUsers(processedMessage);
			PostProcessMessageInput(processedMessage);

			return processedMessage;
		}

		/// <summary>
		/// Some minor housekeeping on the message before we get into the heavy lifting.
		/// </summary>
		ProcessedMessageInput PreProcessMessageInput(string messageBody) {
			var processedMessageInput = new ProcessedMessageInput {
				OriginalBody = messageBody ?? string.Empty,
				DisplayBody = messageBody ?? string.Empty,
				MentionedUsers = new List<string>()
			};

			var displayBody = processedMessageInput.DisplayBody.Trim();

			if (string.IsNullOrEmpty(displayBody))
				throw new Exception("Message body cannot be empty.");

			// make absolutely sure it targets a new window.
			displayBody = new Regex(@"<a ").Replace(displayBody, "<a target='_blank' ");

			// trim extra lines from quotes
			displayBody = new Regex(@"<blockquote>\r*\n*").Replace(displayBody, "<blockquote>");
			displayBody = new Regex(@"\r*\n*</blockquote>\r*\n*").Replace(displayBody, "</blockquote>");

			// keep this as close to the smiley replacement as possible to prevent HTML-izing the bracket.
			displayBody = displayBody.Replace("*heartsmiley*", "<3");

			processedMessageInput.DisplayBody = displayBody;

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

					displayBody = remoteUrlReplacement.Regex.Replace(displayBody, remoteUrlReplacement.ReplacementText, 1);

					displayBody += remoteUrlReplacement.FollowOnText;
				}
			}

			processedMessageInput.DisplayBody = displayBody;
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
				ReplacementText = "$1<a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
				FollowOnText = string.IsNullOrEmpty(remotePageDetails.Card) ? string.Empty : " <br /><br />" + remotePageDetails.Card
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

			var client = new HtmlWeb() {
				UserAgent = "MOZILLA/5.0 (WINDOWS NT 6.1; WOW64) APPLEWEBKIT/537.1 (KHTML, LIKE GECKO) CHROME/21.0.1180.75 SAFARI/537.1"
			};

			client.PreRequest += (handler, request) => {
				request.Headers.ExpectContinue = false;

				handler.CookieContainer = new CookieContainer();
				handler.AllowAutoRedirect = true;
				handler.SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
				handler.MaxAutomaticRedirections = 3;
				handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

				//request.Timeout = 5000;
				return true;
			};

			Task.Run(async () => {
				document = await client.LoadFromWebAsync(siteWithoutHash);
			}).Wait(3000);

			if (document == null) {
				returnResult.Card = "Remote page request timed out.";
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
		async Task FindMentionedUsers(ProcessedMessageInput processedMessageInput) {
			var regexUsers = new Regex(@"@(\S+)");

			foreach (Match regexMatch in regexUsers.Matches(processedMessageInput.DisplayBody)) {
				var matchedTag = regexMatch.Groups[1].Value;

				var user = await DbContext.Users.SingleAsync(u => u.UserName.ToLower() == matchedTag.ToLower());

				// try to guess what they meant
				if (user == null)
					user = await DbContext.Users.SingleAsync(u => u.UserName.ToLower().Contains(matchedTag.ToLower()));

				if (user != null) {
					if (user.Id != UserService.ContextUser.ApplicationUser.Id)
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

		async Task<Message> CreateMessageRecord(ProcessedMessageInput processedMessage, Message replyRecord) {
			var parentId = 0;
			var replyId = 0;

			Message parentMessage = null;

			if (replyRecord != null) {
				if (replyRecord.ParentId == 0) {
					parentId = replyRecord.Id;
					replyId = 0;

					parentMessage = replyRecord;
				}
				else {
					parentId = replyRecord.ParentId;
					replyId = replyRecord.Id;

					parentMessage = await DbContext.Messages.FindAsync(replyRecord.ParentId);

					if (parentMessage == null)
						throw new Exception($"Orphan message found with ID {replyRecord.Id}. Unable to load parent with ID {replyRecord.ParentId}.");
				}
			}

			var currentTime = DateTime.Now;

			var record = new Message {
				OriginalBody = processedMessage.OriginalBody,
				DisplayBody = processedMessage.DisplayBody,
				ShortPreview = processedMessage.ShortPreview,
				LongPreview = processedMessage.LongPreview,

				TimePosted = currentTime,
				TimeEdited = currentTime,
				LastReplyPosted = currentTime,

				PostedById = UserService.ContextUser.ApplicationUser.Id,
				PostedByName = UserService.ContextUser.ApplicationUser.DisplayName,
				EditedById = UserService.ContextUser.ApplicationUser.Id,
				EditedByName = UserService.ContextUser.ApplicationUser.DisplayName,
				LastReplyById = UserService.ContextUser.ApplicationUser.Id,
				LastReplyByName = UserService.ContextUser.ApplicationUser.DisplayName,

				ParentId = parentId,
				ReplyId = replyId,
			};

			DbContext.Messages.Add(record);

			await DbContext.SaveChangesAsync();

			if (replyRecord != null) {
				replyRecord.LastReplyId = record.Id;
				replyRecord.LastReplyById = UserService.ContextUser.ApplicationUser.Id;
				replyRecord.LastReplyByName = UserService.ContextUser.ApplicationUser.DisplayName;
				replyRecord.LastReplyPosted = currentTime;

				DbContext.Entry(replyRecord).State = EntityState.Modified;
			}

			if (parentMessage != null && parentMessage.Id != replyRecord.Id) {
				parentMessage.LastReplyId = record.Id;
				parentMessage.LastReplyById = UserService.ContextUser.ApplicationUser.Id;
				parentMessage.LastReplyByName = UserService.ContextUser.ApplicationUser.DisplayName;
				parentMessage.LastReplyPosted = currentTime;

				DbContext.Entry(parentMessage).State = EntityState.Modified;
			}

			await DbContext.SaveChangesAsync();

			return record;
		}

		async Task UpdateMessageRecord(ProcessedMessageInput message, Message record) {
			record.OriginalBody = message.OriginalBody;
			record.DisplayBody = message.DisplayBody;
			record.ShortPreview = message.ShortPreview;
			record.LongPreview = message.LongPreview;
			record.TimeEdited = DateTime.Now;

			record.EditedById = UserService.ContextUser.ApplicationUser.Id;
			record.EditedByName = UserService.ContextUser.ApplicationUser.DisplayName;

			DbContext.Update(record);

			await DbContext.SaveChangesAsync();
		}
	}
}