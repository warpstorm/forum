using Forum.Contracts;
using Forum.Core;
using Forum.Core.Models.Errors;
using Forum.Core.Options;
using Forum.Data.Contexts;
using Forum.ExternalClients.Imgur;
using Forum.ExternalClients.YouTube;
using Forum.Models.ServiceModels;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Narochno.BBCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum.Services.Repositories {
	using ControllerModels = Models.ControllerModels;
	using DataModels = Data.Models;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class MessageRepository {
		ApplicationDbContext DbContext { get; }
		UserContext CurrentUser { get; }
		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		SmileyRepository SmileyRepository { get; }
		IImageStore ImageStore { get; }
		BBCodeParser BBCParser { get; }
		GzipWebClient WebClient { get; }
		ImgurClient ImgurClient { get; }
		YouTubeClient YouTubeClient { get; }
		ILogger<MessageRepository> Log { get; }

		public MessageRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			BoardRepository boardRepository,
			SmileyRepository smileyRepository,
			IImageStore imageStore,
			BBCodeParser bbcParser,
			GzipWebClient webClient,
			ImgurClient imgurClient,
			YouTubeClient youTubeClient,
			ILogger<MessageRepository> log
		) {
			DbContext = dbContext;
			CurrentUser = userContext;
			AccountRepository = accountRepository;
			BoardRepository = boardRepository;
			SmileyRepository = smileyRepository;
			ImageStore = imageStore;
			BBCParser = bbcParser;
			WebClient = webClient;
			ImgurClient = imgurClient;
			YouTubeClient = youTubeClient;
			Log = log;
		}

		public List<int> GetMessageIds(int topicId, DateTime? fromTime = null) {
			IQueryable<int> messageIdQuery;

			if (fromTime is null) {
				messageIdQuery = from message in DbContext.Messages
								 where message.TopicId == topicId
								 where !message.Deleted
								 select message.Id;
			}
			else {
				messageIdQuery = from message in DbContext.Messages
								 where message.TopicId == topicId
								 where message.TimePosted >= fromTime
								 where !message.Deleted
								 select message.Id;
			}

			return messageIdQuery.ToList();
		}

		public int GetPageNumber(int messageId, List<int> messageIds) {
			var index = (double)messageIds.FindIndex(id => id == messageId);
			index++;

			return Convert.ToInt32(Math.Ceiling(index / CurrentUser.ApplicationUser.MessagesPerPage));
		}

		public async Task<ControllerModels.Messages.CreateReplyResult> CreateReply(ControllerModels.Messages.CreateReplyInput input) {
			var result = new ControllerModels.Messages.CreateReplyResult();

			var topic = await DbContext.Topics.FirstOrDefaultAsync(m => m.Id == input.TopicId);

			if (topic is null || topic.Deleted) {
				result.Errors.Add(nameof(input.TopicId), $"A record does not exist with ID '{input.TopicId}'");
				return result;
			}

			var replyTargetMessage = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			var previousMessage = await DbContext.Messages.FirstOrDefaultAsync(m =>
				m.Id == topic.LastMessageId
				&& m.PostedById == CurrentUser.Id);

			var now = DateTime.Now;
			var recentReply = (now - topic.LastMessageTimePosted) < (now - now.AddSeconds(-300));

			if (recentReply && !(previousMessage is null) && input.Id == previousMessage.ReplyId) {
				return await CreateMergedReply(topic.LastMessageId, input);
			}

			if (result.Errors.Any()) {
				return result;
			}

			var processedMessage = await ProcessMessageInput(input.Body);

			foreach (var error in processedMessage.Errors) {
				result.Errors.Add(error.Key, error.Value);
			}

			if (!result.Errors.Any()) {
				var record = await CreateMessageRecord(processedMessage);
				record.TopicId = topic.Id;
				record.ReplyId = replyTargetMessage?.Id ?? 0;

				DbContext.Update(record);

				topic.ReplyCount++;
				topic.LastMessageId = record.Id;
				topic.LastMessagePostedById = CurrentUser.Id;
				topic.LastMessageTimePosted = now;
				topic.LastMessageShortPreview = record.ShortPreview;

				DbContext.Update(topic);

				if (!(replyTargetMessage is null) && replyTargetMessage.PostedById != CurrentUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = record.Id,
						UserId = replyTargetMessage.PostedById,
						TargetUserId = CurrentUser.Id,
						Time = DateTime.Now,
						Type = ENotificationType.Quote,
						Unread = true,
					};

					DbContext.Notifications.Add(notification);
				}

				await DbContext.SaveChangesAsync();

				UpdateTopicParticipation(topic.Id, CurrentUser.Id, DateTime.Now);

				result.TopicId = record.TopicId;
				result.MessageId = record.Id;
			}

			return result;
		}

		public async Task<ControllerModels.Messages.EditResult> EditMessage(ControllerModels.Messages.EditInput input) {
			var result = new ControllerModels.Messages.EditResult();

			if (input.Id == 0) {
				throw new HttpBadRequestError();
			}

			var record = DbContext.Messages.FirstOrDefault(m => m.Id == input.Id);

			if (record is null || record.Deleted) {
				result.Errors.Add(nameof(input.Id), $"No record found with the ID '{input.Id}'.");
			}

			result.MessageId = record.Id;
			result.TopicId = record.TopicId;

			var processedMessage = await ProcessMessageInput(input.Body);

			foreach (var error in processedMessage.Errors) {
				result.Errors.Add(error.Key, error.Value);
			}

			if (!(result.Errors.Any())) {
				await UpdateMessageRecord(processedMessage, record);

				var topic = DbContext.Topics.Find(record.TopicId);

				if (record.Id == topic.FirstMessageId) {
					topic.FirstMessageShortPreview = record.ShortPreview;
					DbContext.Update(topic);
				}

				if (record.Id == topic.LastMessageId) {
					topic.LastMessageShortPreview = record.ShortPreview;
					DbContext.Update(topic);
				}

				await DbContext.SaveChangesAsync();
			}

			return result;
		}

		async Task<ControllerModels.Messages.CreateReplyResult> CreateMergedReply(int previousMessageId, ControllerModels.Messages.CreateReplyInput input) {
			var result = new ControllerModels.Messages.CreateReplyResult {
				Merged = true
			};

			var previousMessage = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == previousMessageId && !m.Deleted);
			var newBody = $"{previousMessage.OriginalBody}\n\n{input.Body}";

			var processedMessage = await ProcessMessageInput(newBody);

			foreach (var error in processedMessage.Errors) {
				result.Errors.Add(error.Key, error.Value);
			}

			if (!(result.Errors.Any())) {
				await UpdateMessageRecord(processedMessage, previousMessage);

				result.MessageId = previousMessage.Id;
				result.TopicId = previousMessage.TopicId;
			}

			return result;
		}

		public async Task<ControllerModels.Messages.AddThoughtResult> AddThought(int messageId, int smileyId) {
			var result = new ControllerModels.Messages.AddThoughtResult();

			var messageRecord = DbContext.Messages.Find(messageId);

			if (messageRecord is null || messageRecord.Deleted) {
				result.Errors.Add(string.Empty, $@"No message was found with the id '{messageId}'");
			}

			var smileyRecord = await DbContext.Smileys.FindAsync(smileyId);

			if (messageRecord is null) {
				result.Errors.Add(string.Empty, $@"No smiley was found with the id '{smileyId}'");
			}

			if (result.Errors.Any()) {
				return result;
			}

			var existingRecord = await DbContext.MessageThoughts
				.FirstOrDefaultAsync(mt =>
					mt.MessageId == messageRecord.Id
					&& mt.SmileyId == smileyRecord.Id
					&& mt.UserId == CurrentUser.Id);

			if (existingRecord is null) {
				var messageThought = new DataModels.MessageThought {
					MessageId = messageRecord.Id,
					SmileyId = smileyRecord.Id,
					UserId = CurrentUser.Id
				};

				DbContext.MessageThoughts.Add(messageThought);

				if (messageRecord.PostedById != CurrentUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = messageRecord.Id,
						UserId = messageRecord.PostedById,
						TargetUserId = CurrentUser.Id,
						Time = DateTime.Now,
						Type = ENotificationType.Thought,
						Unread = true,
					};

					DbContext.Notifications.Add(notification);
				}
			}
			else {
				DbContext.Remove(existingRecord);

				var notification = await DbContext.Notifications.FirstOrDefaultAsync(item => item.MessageId == existingRecord.MessageId && item.TargetUserId == existingRecord.UserId && item.Type == ENotificationType.Thought);

				if (notification != null) {
					DbContext.Remove(notification);
				}
			}

			await DbContext.SaveChangesAsync();

			result.TopicId = messageRecord.TopicId;
			result.MessageId = messageRecord.Id;

			return result;
		}

		public async Task<InputModels.ProcessedMessageInput> ProcessMessageInput(string messageBody) {
			var processedMessage = new InputModels.ProcessedMessageInput {
				OriginalBody = messageBody ?? string.Empty,
				DisplayBody = messageBody ?? string.Empty,
				MentionedUsers = new List<string>()
			};

			try {
				processedMessage = await PreProcess(processedMessage);
				await ProcessSmileys(processedMessage);
				await ProcessMessageBodyUrls(processedMessage);
				await FindMentionedUsers(processedMessage);
				PostProcessMessageInput(processedMessage);
			}
			catch (ArgumentException ex) {
				processedMessage.Errors.Add("Body", $"An error occurred while processing the message. {ex.Message}");
			}

			if (processedMessage is null) {
				processedMessage.Errors.Add("Body", $"An error occurred while processing the message.");
				return processedMessage;
			}

			return processedMessage;
		}

		/// <summary>
		/// Some minor housekeeping on the message before we get into the heavy lifting.
		/// </summary>
		public async Task<InputModels.ProcessedMessageInput> PreProcess(InputModels.ProcessedMessageInput processedMessage) {
			processedMessage.DisplayBody = processedMessage.DisplayBody.Trim();

			if (string.IsNullOrEmpty(processedMessage.DisplayBody)) {
				throw new ArgumentException("Message body is empty.");
			}

			var smileys = await SmileyRepository.Records();

			// Ensures the smileys are safe from other HTML processing.
			for (var i = 0; i < smileys.Count(); i++) {
				var pattern = $@"(^|[\r\n\s]){Regex.Escape(smileys[i].Code)}(?=$|[\r\n\s])";
				var replacement = $"$1SMILEY_{i}_INDEX";
				processedMessage.DisplayBody = Regex.Replace(processedMessage.DisplayBody, pattern, replacement, RegexOptions.Singleline);
			}

			processedMessage.DisplayBody = BBCParser.ToHtml(processedMessage.DisplayBody);

			return processedMessage;
		}

		/// <summary>
		/// Attempt to replace URLs in the message body with something better
		/// </summary>
		public async Task ProcessMessageBodyUrls(InputModels.ProcessedMessageInput processedMessageInput) {
			var displayBody = processedMessageInput.DisplayBody;

			var regexUrl = new Regex("(^| )((https?\\://){1}\\S+)", RegexOptions.Compiled | RegexOptions.Multiline);

			var matches = 0;
			var replacements = new Dictionary<string, string>();

			foreach (Match regexMatch in regexUrl.Matches(displayBody)) {
				matches++;

				// DoS prevention
				if (matches > 10) {
					break;
				}

				var siteUrl = regexMatch.Groups[2].Value;

				if (string.IsNullOrEmpty(siteUrl)) {
					continue;
				}

				var key = $"SITEURL_{matches}";
				displayBody = displayBody.Replace(siteUrl, key);

				var remoteUrlReplacement = await GetRemoteUrlReplacement(siteUrl);

				if (remoteUrlReplacement != null) {
					replacements.Add(key, remoteUrlReplacement.ReplacementText);
					processedMessageInput.Cards += remoteUrlReplacement.Card;
				}
			}

			foreach (var kvp in replacements) {
				displayBody = displayBody.Replace(kvp.Key, kvp.Value);
			}

			processedMessageInput.DisplayBody = displayBody;
		}

		public async Task ProcessSmileys(InputModels.ProcessedMessageInput processedMessageInput) {
			var smileys = await SmileyRepository.Records();

			for (var i = 0; i < smileys.Count(); i++) {
				var pattern = $@"SMILEY_{i}_INDEX";
				var replacement = $"<img src='{smileys[i].Path}' />";
				processedMessageInput.DisplayBody = Regex.Replace(processedMessageInput.DisplayBody, pattern, replacement);
			}
		}

		/// <summary>
		/// Attempt to replace the ugly URL with a human readable title.
		/// </summary>
		public async Task<IUrlReplacement> GetRemoteUrlReplacement(string remoteUrl) {
			var remotePageDetails = await GetRemotePageDetails(remoteUrl);
			remotePageDetails.Title = remotePageDetails.Title.Replace("$", "&#36;");

			var favicon = string.Empty;

			if (!string.IsNullOrEmpty(remotePageDetails.Favicon)) {
				favicon = $"<img class='link-favicon' src='{remotePageDetails.Favicon}' /> ";
			}


			if (YouTubeClient.TryGetReplacement(remoteUrl, remotePageDetails.Title, favicon, out var replacement)) {
				return replacement;
			}

			if (ImgurClient.TryGetReplacement(remoteUrl, remotePageDetails.Title, favicon, out replacement)) {
				return replacement;
			}

			// replace the URL with the HTML
			return new UrlReplacement {
				ReplacementText = $"<a target='_blank' href='{remoteUrl}'>{favicon}{remotePageDetails.Title}</a>",
				Card = remotePageDetails.Card ?? string.Empty
			};
		}

		/// <summary>
		/// I really should make this async. Load a remote page by URL and attempt to get details about it.
		/// </summary>
		public async Task<ServiceModels.RemotePageDetails> GetRemotePageDetails(string remoteUrl) {
			var returnResult = new ServiceModels.RemotePageDetails {
				Title = remoteUrl,
			};

			Uri uri;

			try {
				uri = new Uri(remoteUrl);
			}
			catch (UriFormatException ex) {
				var logUrl = remoteUrl.Substring(0, 255);
				Log.LogWarning(ex, $"{nameof(GetRemotePageDetails)} couldn't create a URI from -- {logUrl}");
				return returnResult;
			}

			var remoteUrlAuthority = uri.GetLeftPart(UriPartial.Authority);
			var domain = uri.Host.Replace("/www.", "/").ToLower();

			var faviconPath = $"{remoteUrlAuthority}/favicon.ico";
			var faviconStoragePath = await CacheFavicon(domain, uri.GetLeftPart(UriPartial.Path), faviconPath);

			var document = WebClient.DownloadDocument(remoteUrl);

			if (document is null) {
				return returnResult;
			}

			var titleTag = document.DocumentNode.SelectSingleNode(@"//title");

			if (titleTag != null && !string.IsNullOrEmpty(titleTag.InnerText.Trim())) {
				returnResult.Title = titleTag.InnerText.Trim();
			}

			if (string.IsNullOrEmpty(faviconStoragePath)) {
				var element = document.DocumentNode.SelectSingleNode(@"//link[@rel='shortcut icon']");

				if (element != null) {
					faviconPath = element.Attributes["href"].Value.Trim();
					faviconStoragePath = await CacheFavicon(domain, uri.GetLeftPart(UriPartial.Path), faviconPath);
				}
			}

			if (string.IsNullOrEmpty(faviconStoragePath)) {
				var element = document.DocumentNode.SelectSingleNode(@"//link[@rel='icon']");

				if (element != null) {
					faviconPath = element.Attributes["href"].Value.Trim();
					faviconStoragePath = await CacheFavicon(domain, uri.GetLeftPart(UriPartial.Path), faviconPath);
				}
			}

			returnResult.Favicon = faviconStoragePath;

			ServiceModels.OgDetails ogDetails = null;

			if (domain == "warpstorm.com" || domain == "localhost") {
				ogDetails = GetWarpstormOgDetails(remoteUrl);
			}
			else {
				ogDetails = GetOgDetails(document);
			}

			if (ogDetails != null) {
				returnResult.Title = ogDetails.Title;

				if (!string.IsNullOrEmpty(ogDetails.Description)) {
					returnResult.Card += "<blockquote class='card hover-highlight' clickable-link-parent>";

					if (!string.IsNullOrEmpty(ogDetails.Image)) {
						if (ogDetails.Image.StartsWith("/")) {
							ogDetails.Image = $"{remoteUrlAuthority}{ogDetails.Image}";
						}

						returnResult.Card += $"<div class='card-image'><img src='{ogDetails.Image}' /></div>";
					}

					returnResult.Card += "<div>";
					returnResult.Card += $"<p class='card-title'><a target='_blank' href='{remoteUrl}'>{returnResult.Title}</a></p>";

					var decodedDescription = WebUtility.HtmlDecode(ogDetails.Description);

					returnResult.Card += $"<p class='card-description'>{decodedDescription}</p>";

					if (string.IsNullOrEmpty(ogDetails.SiteName)) {
						returnResult.Card += $"<p class='card-link'><a target='_blank' href='{remoteUrl}'>[Direct Link]</a></p>";
					}
					else {
						returnResult.Card += $"<p class='card-link'><a target='_blank' href='{remoteUrl}'>[{ogDetails.SiteName}]</a></p>";
					}

					returnResult.Card += "</div><br class='clear' /></blockquote>";
				}
			}

			if (returnResult.Title.Contains(" - ")) {
				StripTitleSiteName(domain, returnResult);
			}

			return returnResult;
		}

		ServiceModels.OgDetails GetWarpstormOgDetails(string remoteUrl) {
			var topicIdMatch = Regex.Match(remoteUrl, @"Topics\/(Display|Latest)\/(\d+)\/?(\d+|)\/?(\d+|)(#message(\d+))?");

			if (!topicIdMatch.Success) {
				return null;
			}

			var messageId = 0;

			if (string.IsNullOrEmpty(topicIdMatch.Groups[6].Value)) {
				messageId = Convert.ToInt32(topicIdMatch.Groups[2].Value);
			}
			else {
				messageId = Convert.ToInt32(topicIdMatch.Groups[6].Value);
			}

			var messageRecordQuery = from message in DbContext.Messages
									 where message.Id == messageId
									 where !message.Deleted
									 select new {
										 message.ShortPreview,
										 message.LongPreview
									 };

			var messageRecord = messageRecordQuery.FirstOrDefault();

			if (messageRecord is null) {
				return null;
			}

			return new ServiceModels.OgDetails {
				Title = messageRecord.ShortPreview,
				Description = messageRecord.LongPreview,
				Image = "/images/logos/planet.png",
				SiteName = "Warpstorm"
			};
		}

		ServiceModels.OgDetails GetOgDetails(HtmlDocument document) {
			var returnObject = new ServiceModels.OgDetails();

			var titleNode = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:title']");

			if (titleNode != null && titleNode.Attributes["content"] != null) {
				returnObject.Title = titleNode.Attributes["content"].Value.Trim();
			}

			var descriptionNode = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:description']");

			if (descriptionNode != null && descriptionNode.Attributes["content"] != null) {
				returnObject.Description = descriptionNode.Attributes["content"].Value.Trim();
			}

			var siteNameNode = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:site_name']");

			if (siteNameNode != null && siteNameNode.Attributes["content"] != null) {
				returnObject.SiteName = siteNameNode.Attributes["content"].Value.Trim();
			}

			var imageNode = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:image']");

			if (imageNode != null && imageNode.Attributes["content"] != null) {
				returnObject.Image = imageNode.Attributes["content"].Value.Trim();

				// Twitch og:image starts without a protocol.
				if (returnObject.Image.StartsWith("//")) {
					returnObject.Image = $"https:{returnObject.Image}";
				}
			}

			if (string.IsNullOrEmpty(returnObject.Title)) {
				return null;
			}

			return returnObject;
		}

		void StripTitleSiteName(string domain, ServiceModels.RemotePageDetails returnResult) {
			var secondLevelDomainMatches = Regex.Match(domain, @"([^.]*)\.[^.]{2,3}(?:\.[^.]{2,3})?$", RegexOptions.IgnoreCase);

			if (secondLevelDomainMatches.Success) {
				var strippedUrl = DbContext.StrippedUrls.FirstOrDefault(r => r.Url == secondLevelDomainMatches.Groups[1].Value);

				if (!(strippedUrl is null)) {
					var match = Regex.Match(returnResult.Title, strippedUrl.RegexPattern);
					returnResult.Title = match.Groups[1].Value;
				}
			}
		}

		/// <summary>
		/// Loads the favicon.ico file from a remote site, stores it in Azure, and returns the path to the Azure cached image.
		/// </summary>
		public async Task<string> CacheFavicon(string domain, string remoteUrlBase, string faviconPath) {
			try {
				if (!Uri.TryCreate(faviconPath, UriKind.Absolute, out var faviconUri)) {
					var baseUri = new Uri(remoteUrlBase, UriKind.Absolute);
					faviconUri = new Uri(baseUri, faviconPath);
				}

				var webRequest = WebRequest.Create(faviconUri);

				using var webResponse = webRequest.GetResponse();
				using var inputStream = webResponse.GetResponseStream();

				return await ImageStore.Save(new ImageStoreSaveOptions {
					ContainerName = "favicons",
					FileName = $"{domain}.png",
					ContentType = "image/png",
					InputStream = inputStream,
					MaxDimension = 16
				});
			}
			catch (Exception ex) {
				Log.LogError(ex, $"{nameof(CacheFavicon)} threw an exception.");
			}

			return string.Empty;
		}

		/// <summary>
		/// Searches a post for references to other users
		/// </summary>
		public async Task FindMentionedUsers(InputModels.ProcessedMessageInput processedMessageInput) {
			var regexUsers = new Regex(@"@(\S+)");

			var matches = 0;

			foreach (Match regexMatch in regexUsers.Matches(processedMessageInput.DisplayBody)) {
				matches++;

				// DoS prevention
				if (matches > 10) {
					break;
				}

				var matchedTag = regexMatch.Groups[1].Value;

				var users = await AccountRepository.Records();

				var user = users.FirstOrDefault(u => u.DisplayName.ToLower() == matchedTag.ToLower());

				// try to guess what they meant
				if (user is null) {
					user = users.FirstOrDefault(u => u.UserName.ToLower().Contains(matchedTag.ToLower()));
				}

				if (user != null) {
					if (user.Id != CurrentUser.Id) {
						processedMessageInput.MentionedUsers.Add(user.Id);
					}

					// UNTESTED
					//processedMessageInput.DisplayBody = processedMessageInput.DisplayBody.Replace($"{regexMatch.Groups[1].Value}", $"<a href='/Messages/History/{user.Id}'>{user.DisplayName}</span>");
				}
			}
		}

		/// <summary>
		/// Minor post processing
		/// </summary>
		public void PostProcessMessageInput(InputModels.ProcessedMessageInput processedMessageInput) {
			// make absolutely sure it targets a new window.
			processedMessageInput.DisplayBody = new Regex(@"<a ").Replace(processedMessageInput.DisplayBody, "<a target='_blank' ");

			// trim extra lines from quotes
			processedMessageInput.DisplayBody = new Regex(@"<blockquote(.*?)>(\r|\n|\r\n)*").Replace(processedMessageInput.DisplayBody, match => $"<blockquote{match.Groups[1].Value}>");
			processedMessageInput.DisplayBody = new Regex(@"(\r|\n|\r\n)*</blockquote>(\r|\n|\r\n)*").Replace(processedMessageInput.DisplayBody, "</blockquote>");

			processedMessageInput.DisplayBody = processedMessageInput.DisplayBody.Trim();
			processedMessageInput.ShortPreview = GetMessagePreview(processedMessageInput.DisplayBody, 100);
			processedMessageInput.LongPreview = GetMessagePreview(processedMessageInput.DisplayBody, 500, true);
		}

		/// <summary>
		/// Gets a reduced version of the message without HTML
		/// </summary>
		public string GetMessagePreview(string messageBody, int previewLength, bool multiline = false) {
			var preview = messageBody;

			// strip out quotes
			preview = Regex.Replace(preview, @"(<blockquote.*?>.+?</blockquote>\n*?)", string.Empty, RegexOptions.Compiled);

			// strip out spoilers
			preview = Regex.Replace(preview, @"<.+?bbc-spoiler.+?>.+?</.+?>", string.Empty, RegexOptions.Compiled);

			// strip out tags
			preview = Regex.Replace(preview, @"(<.+?>|\[.+?\])", string.Empty, RegexOptions.Compiled);

			if (!multiline) {
				preview = Regex.Match(preview, @"^(.+)?(\r|\n|\r\n|)*", RegexOptions.Compiled).Groups[1].Value;
			}

			if (preview.Length > previewLength) {
				var matches = Regex.Match(preview, @"^(.{" + (previewLength - 1) + "})", RegexOptions.Compiled);
				preview = matches.Groups[1].Value + "…";
			}
			else if (preview.Length <= 0) {
				preview = "No text";
			}

			preview = preview.Trim();

			return preview;
		}

		public async Task<DataModels.Message> CreateMessageRecord(InputModels.ProcessedMessageInput processedMessage) {
			var now = DateTime.Now;

			var record = new DataModels.Message {
				OriginalBody = processedMessage.OriginalBody,
				DisplayBody = processedMessage.DisplayBody,
				ShortPreview = processedMessage.ShortPreview,
				LongPreview = processedMessage.LongPreview,
				Cards = processedMessage.Cards,
				TimePosted = now,
				TimeEdited = now,
				PostedById = CurrentUser.Id,
			};

			DbContext.Messages.Add(record);
			await DbContext.SaveChangesAsync();

			await NotifyMentionedUsers(processedMessage.MentionedUsers, record.Id);

			return record;
		}

		public async Task UpdateMessageRecord(InputModels.ProcessedMessageInput message, DataModels.Message record) {
			record.OriginalBody = message.OriginalBody;
			record.DisplayBody = message.DisplayBody;
			record.ShortPreview = message.ShortPreview;
			record.LongPreview = message.LongPreview;
			record.Cards = message.Cards;
			record.TimeEdited = DateTime.Now;

			DbContext.Update(record);

			await DbContext.SaveChangesAsync();
		}

		public void UpdateTopicParticipation(int topicId, string userId, DateTime time) {
			var participation = DbContext.Participants.FirstOrDefault(r => r.TopicId == topicId && r.UserId == userId);

			if (participation is null) {
				DbContext.Participants.Add(new DataModels.Participant {
					TopicId = topicId,
					UserId = userId,
					Time = time
				});
			}
			else {
				participation.Time = time;
				DbContext.Update(participation);
			}
		}

		public async Task NotifyMentionedUsers(List<string> mentionedUsers, int messageId) {
			foreach (var user in mentionedUsers) {
				var notification = new DataModels.Notification {
					MessageId = messageId,
					TargetUserId = CurrentUser.Id,
					UserId = user,
					Time = DateTime.Now,
					Type = ENotificationType.Mention,
					Unread = true
				};

				DbContext.Notifications.Add(notification);
			}

			await DbContext.SaveChangesAsync();
		}

		public async Task DeleteMessageFromTopic(DataModels.Message message) {
			var directRepliesQuery = from m in DbContext.Messages
									 where m.ReplyId == message.Id
									 where !m.Deleted
									 select m;

			foreach (var reply in directRepliesQuery) {
				reply.OriginalBody =
					$"[quote]{message.OriginalBody}\n" +
					$"Message deleted by {CurrentUser.ApplicationUser.DisplayName} on {DateTime.Now.ToString("MMMM dd, yyyy")}[/quote]" +
					reply.OriginalBody;

				reply.ReplyId = 0;

				DbContext.Update(reply);
			}

			message.Deleted = true;
			DbContext.Update(message);
			await DbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Builds a collection of Message objects. The message ids should already have been filtered by permissions.
		/// </summary>
		public async Task<List<ViewModels.Messages.DisplayMessage>> GetMessages(List<int> messageIds) {
			var thoughtQuery = from mt in DbContext.MessageThoughts
							   where messageIds.Contains(mt.MessageId)
							   select new ViewModels.Messages.MessageThought {
								   MessageId = mt.MessageId.ToString(),
								   UserId = mt.UserId,
								   SmileyId = mt.SmileyId,
							   };

			var thoughts = thoughtQuery.ToList();
			var smileys = await SmileyRepository.Records();
			var users = await AccountRepository.Records();

			foreach (var item in thoughts) {
				var smiley = smileys.FirstOrDefault(r => r.Id == item.SmileyId);
				var user = users.FirstOrDefault(r => r.Id == item.UserId);

				item.Path = smiley.Path;
				item.Thought = smiley.Thought.Replace("{user}", user.DecoratedName);
			}

			var messageQuery = from message in DbContext.Messages
							   where messageIds.Contains(message.Id)
							   select new ViewModels.Messages.DisplayMessage {
								   Id = message.Id.ToString(),
								   TopicId = message.TopicId,
								   ReplyId = message.ReplyId,
								   Body = message.DisplayBody,
								   Cards = message.Cards,
								   OriginalBody = message.OriginalBody,
								   PostedById = message.PostedById,
								   TimePosted = message.TimePosted,
								   TimeEdited = message.TimeEdited,
								   RecordTime = message.TimeEdited
							   };

			var messages = await messageQuery.ToListAsync();

			foreach (var message in messages) {
				message.ShowControls = true;

				if (message.ReplyId > 0) {
					var reply = DbContext.Messages.FirstOrDefault(item => item.Id == message.ReplyId && !item.Deleted);

					if (!(reply is null)) {
						if (string.IsNullOrEmpty(reply.ShortPreview)) {
							reply.ShortPreview = "No preview";
						}

						message.ReplyBody = reply.DisplayBody;
						message.ReplyPreview = reply.ShortPreview;

						var replyPostedBy = users.FirstOrDefault(item => item.Id == reply.PostedById);
						message.ReplyPostedBy = replyPostedBy?.DecoratedName ?? "A user";
					}
				}

				var topic = DbContext.Topics.Find(message.TopicId);

				message.IsFirstMessage = topic.FirstMessageId.ToString() == message.Id;
				message.CanEdit = CurrentUser.IsAdmin || CurrentUser.Id == message.PostedById;
				message.CanDelete = CurrentUser.IsAdmin || CurrentUser.Id == message.PostedById;
				message.CanReply = CurrentUser.IsAuthenticated;
				message.CanThought = CurrentUser.IsAuthenticated;
				message.CanQuote = CurrentUser.IsAuthenticated;
				message.Thoughts = thoughts.Where(item => item.MessageId == message.Id).ToList();

				var postedBy = users.FirstOrDefault(item => item.Id == message.PostedById);

				if (!(postedBy is null)) {
					message.PostedByAvatarPath = postedBy.AvatarPath;
					message.PostedByName = postedBy.DecoratedName;
					message.Poseys = postedBy.Poseys;

					if (DateTime.Now.Date == new DateTime(DateTime.Now.Year, postedBy.Birthday.Month, postedBy.Birthday.Day).Date) {
						message.Birthday = true;
					}
				}
			}

			return messages;
		}

		public async Task<List<ViewModels.Messages.DisplayMessage>> GetUserMessages(string userId, int page) {
			var take = CurrentUser.ApplicationUser.MessagesPerPage;
			var skip = (page - 1) * take;

			var messageQuery = from message in DbContext.Messages
							   where message.PostedById == userId
							   where !message.Deleted
							   orderby message.Id descending
							   select new {
								   message.Id,
								   message.TopicId
							   };

			var messageIds = new List<int>();
			var attempts = 0;
			var skipped = 0;

			foreach (var message in messageQuery) {
				if (!await BoardRepository.CanAccess(message.TopicId)) {
					if (attempts++ > 100) {
						break;
					}

					continue;
				}

				if (skipped++ < skip) {
					continue;
				}

				messageIds.Add(message.Id);

				if (messageIds.Count == take) {
					break;
				}
			}

			var messages = await GetMessages(messageIds);

			foreach (var message in messages) {
				message.ShowControls = false;
			}

			return messages;
		}
	}
}