﻿using CodeKicker.BBCode;
using Forum.Contexts;
using Forum.Controllers;
using Forum.Errors;
using Forum.Extensions;
using Forum.Plugins.ImageStore;
using Forum.Plugins.UrlReplacement;
using Forum.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;

	public class MessageRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		BoardRepository BoardRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		AccountRepository AccountRepository { get; }
		IImageStore ImageStore { get; }
		IUrlHelper UrlHelper { get; }
		BBCodeParser BBCParser { get; }
		GzipWebClient WebClient { get; }
		ImgurClient ImgurClient { get; }
		YouTubeClient YouTubeClient { get; }

		public MessageRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			BoardRepository boardRepository,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			AccountRepository accountRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory,
			IImageStore imageStore,
			BBCodeParser bbcParser,
			GzipWebClient webClient,
			ImgurClient imgurClient,
			YouTubeClient youTubeClient
		) {
			DbContext = dbContext;
			UserContext = userContext;
			BoardRepository = boardRepository;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			AccountRepository = accountRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
			ImageStore = imageStore;
			BBCParser = bbcParser;
			WebClient = webClient;
			ImgurClient = imgurClient;
			YouTubeClient = youTubeClient;
		}

		public int GetPageNumber(int messageId, List<int> messageIds) {
			var index = (double)messageIds.FindIndex(id => id == messageId);
			index++;

			var messagesPerPage = SettingsRepository.MessagesPerPage();
			return Convert.ToInt32(Math.Ceiling(index / messagesPerPage));
		}

		public async Task<ServiceModels.ServiceResponse> CreateTopic(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var processedMessage = await ProcessMessageInput(serviceResponse, input.Body);

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var record = CreateMessageRecord(processedMessage, null);

			var existingMessageBoards = DbContext.MessageBoards.Where(item => item.MessageId == record.Id).ToList();

			foreach (var item in existingMessageBoards) {
				DbContext.Remove(item);
			}

			foreach (var selectedBoard in input.SelectedBoards) {
				var board = BoardRepository.FirstOrDefault(item => item.Id == selectedBoard);

				if (board != null) {
					DbContext.MessageBoards.Add(new DataModels.MessageBoard {
						MessageId = record.Id,
						BoardId = board.Id,
						TimeAdded = DateTime.Now,
						UserId = UserContext.ApplicationUser.Id
					});
				}
			}

			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> CreateReply(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Id == 0) {
				throw new HttpBadRequestError();
			}

			var replyRecord = DbContext.Messages.FirstOrDefault(m => m.Id == input.Id);

			if (replyRecord is null) {
				serviceResponse.Error($"A record does not exist with ID '{input.Id}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var processedMessage = await ProcessMessageInput(serviceResponse, input.Body);

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var record = CreateMessageRecord(processedMessage, replyRecord);

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> EditMessage(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Id == 0) {
				throw new HttpBadRequestError();
			}

			var record = DbContext.Messages.FirstOrDefault(m => m.Id == input.Id);

			if (record is null) {
				serviceResponse.Error(nameof(input.Id), $"No record found with the ID '{input.Id}'.");
			}

			var processedMessage = await ProcessMessageInput(serviceResponse, input.Body);

			if (serviceResponse.Success) {
				serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
				UpdateMessageRecord(processedMessage, record);
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> DeleteMessage(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = DbContext.Messages.FirstOrDefault(m => m.Id == messageId);

			if (record is null) {
				serviceResponse.Error($@"No record was found with the id '{messageId}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var parentId = record.ParentId;

			if (parentId != 0) {
				serviceResponse.RedirectPath = UrlHelper.DirectMessage(parentId);

				var directReplies = await DbContext.Messages.Where(m => m.ReplyId == messageId).ToListAsync();

				foreach (var reply in directReplies) {
					reply.OriginalBody =
						$"[quote]{record.OriginalBody}\n" +
						$"Message deleted by {UserContext.ApplicationUser.DisplayName} on {DateTime.Now.ToString("MMMM dd, yyyy")}[/quote]" +
						reply.OriginalBody;

					reply.ReplyId = 0;

					DbContext.Update(reply);
				}

				DbContext.SaveChanges();
			}
			else {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Index), nameof(Topics));
			}

			var topicReplies = await DbContext.Messages.Where(m => m.ParentId == messageId).ToListAsync();

			foreach (var reply in topicReplies) {
				await RemoveMessageArtifacts(reply);
			}

			await RemoveMessageArtifacts(record);

			DbContext.SaveChanges();

			if (parentId > 0) {
				var parent = DbContext.Messages.FirstOrDefault(item => item.Id == parentId);

				if (parent != null) {
					RecountRepliesForTopic(parent);
				}
			}

			return serviceResponse;
		}

		async Task RemoveMessageArtifacts(DataModels.Message record) {
			var messageBoards = await DbContext.MessageBoards.Where(m => m.MessageId == record.Id).ToListAsync();

			foreach (var messageBoard in messageBoards) {
				DbContext.MessageBoards.Remove(messageBoard);
			}

			var messageThoughts = await DbContext.MessageThoughts.Where(mt => mt.MessageId == record.Id).ToListAsync();

			foreach (var messageThought in messageThoughts) {
				DbContext.MessageThoughts.Remove(messageThought);
			}

			var notifications = DbContext.Notifications.Where(item => item.MessageId == record.Id).ToList();

			foreach (var notification in notifications) {
				DbContext.Notifications.Remove(notification);
			}

			DbContext.Messages.Remove(record);
		}

		public async Task<ServiceModels.ServiceResponse> AddThought(InputModels.ThoughtInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = DbContext.Messages.Find(input.MessageId);

			if (messageRecord is null) {
				serviceResponse.Error($@"No message was found with the id '{input.MessageId}'");
			}

			var smileyRecord = await DbContext.Smileys.FindAsync(input.SmileyId);

			if (messageRecord is null) {
				serviceResponse.Error($@"No smiley was found with the id '{input.SmileyId}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var existingRecord = await DbContext.MessageThoughts
				.SingleOrDefaultAsync(mt =>
					mt.MessageId == messageRecord.Id
					&& mt.SmileyId == smileyRecord.Id
					&& mt.UserId == UserContext.ApplicationUser.Id);

			if (existingRecord is null) {
				var messageThought = new DataModels.MessageThought {
					MessageId = messageRecord.Id,
					SmileyId = smileyRecord.Id,
					UserId = UserContext.ApplicationUser.Id
				};

				DbContext.MessageThoughts.Add(messageThought);

				if (messageRecord.PostedById != UserContext.ApplicationUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = messageRecord.Id,
						UserId = messageRecord.PostedById,
						TargetUserId = UserContext.ApplicationUser.Id,
						Time = DateTime.Now,
						Type = Enums.ENotificationType.Thought,
						Unread = true,
					};

					DbContext.Notifications.Add(notification);
				}
			}
			else {
				DbContext.Remove(existingRecord);

				var notification = DbContext.Notifications.FirstOrDefault(item => item.MessageId == existingRecord.MessageId && item.TargetUserId == existingRecord.UserId && item.Type == Enums.ENotificationType.Thought);

				if (notification != null) {
					DbContext.Remove(notification);
				}
			}

			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(input.MessageId);
			return serviceResponse;
		}

		public async Task<InputModels.ProcessedMessageInput> ProcessMessageInput(ServiceModels.ServiceResponse serviceResponse, string messageBody) {
			InputModels.ProcessedMessageInput processedMessage = null;

			try {
				processedMessage = PreProcessMessageInput(messageBody);
				ParseBBC(processedMessage);
				ProcessSmileys(processedMessage);
				await ProcessMessageBodyUrls(processedMessage);
				FindMentionedUsers(processedMessage);
				PostProcessMessageInput(processedMessage);
			}
			catch (ArgumentException e) {
				serviceResponse.Error(nameof(InputModels.MessageInput.Body), $"An error occurred while processing the message. {e.Message}");
			}

			if (processedMessage is null) {
				serviceResponse.Error(nameof(InputModels.MessageInput.Body), $"An error occurred while processing the message.");
				return processedMessage;
			}

			return processedMessage;
		}

		/// <summary>
		/// Some minor housekeeping on the message before we get into the heavy lifting.
		/// </summary>
		public InputModels.ProcessedMessageInput PreProcessMessageInput(string messageBody) {
			var processedMessageInput = new InputModels.ProcessedMessageInput {
				OriginalBody = messageBody ?? string.Empty,
				DisplayBody = messageBody ?? string.Empty,
				MentionedUsers = new List<string>()
			};

			processedMessageInput.DisplayBody = processedMessageInput.DisplayBody.Trim();

			if (string.IsNullOrEmpty(processedMessageInput.DisplayBody)) {
				throw new ArgumentException("Message body is empty.");
			}

			// Ensures the smileys are safe from other HTML processing.
			for (var i = 0; i < SmileyRepository.Count(); i++) {
				var pattern = $@"(^|[\r\n\s]){Regex.Escape(SmileyRepository[i].Code)}(?=$|[\r\n\s])";
				var replacement = $"$1SMILEY_{i}_INDEX";
				processedMessageInput.DisplayBody = Regex.Replace(processedMessageInput.DisplayBody, pattern, replacement, RegexOptions.Singleline);
			}

			return processedMessageInput;
		}

		public void ParseBBC(InputModels.ProcessedMessageInput processedMessageInput) {

			// NOTE: 
			// If you do not want to encumber your software with the CodeKicker library, start with replacing this call.

			processedMessageInput.DisplayBody = BBCParser.ToHtml(processedMessageInput.DisplayBody);
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

		public void ProcessSmileys(InputModels.ProcessedMessageInput processedMessageInput) {
			for (var i = 0; i < SmileyRepository.Count(); i++) {
				var pattern = $@"SMILEY_{i}_INDEX";
				var replacement = $"<img src='{SmileyRepository[i].Path}' />";
				processedMessageInput.DisplayBody = Regex.Replace(processedMessageInput.DisplayBody, pattern, replacement);
			}
		}

		/// <summary>
		/// Attempt to replace the ugly URL with a human readable title.
		/// </summary>
		public async Task<UrlReplacement> GetRemoteUrlReplacement(string remoteUrl) {
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
			var uri = new Uri(remoteUrl);
			var remoteUrlAuthority = uri.GetLeftPart(UriPartial.Authority);
			var domain = uri.Host.Replace("/www.", "/").ToLower();

			var faviconPath = $"{remoteUrlAuthority}/favicon.ico";
			var faviconStoragePath = await CacheFavicon(domain, uri.GetLeftPart(UriPartial.Path), faviconPath);

			var returnResult = new ServiceModels.RemotePageDetails {
				Title = remoteUrl,
			};

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
					returnResult.Card += "<blockquote class='card pointer hover-highlight' clickable-link-parent>";

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
			}

			if (string.IsNullOrEmpty(returnObject.Title)) {
				return null;
			}

			return returnObject;
		}

		void StripTitleSiteName(string domain, ServiceModels.RemotePageDetails returnResult) {
			var secondLevelDomainMatches = Regex.Match(domain, @"([^.]*)\.[^.]{2,3}(?:\.[^.]{2,3})?$", RegexOptions.IgnoreCase);

			if (secondLevelDomainMatches.Success) {
				var strippedUrls = SettingsRepository.StrippedUrls();

				if (strippedUrls.Contains(secondLevelDomainMatches.Groups[1].Value)) {
					returnResult.Title = returnResult.Title.Split(" - ")[0];
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

				using (var webResponse = webRequest.GetResponse())
				using (var inputStream = webResponse.GetResponseStream()) {
					return await ImageStore.Save(new ImageStoreSaveOptions {
						ContainerName = "favicons",
						FileName = $"{domain}.png",
						ContentType = "image/png",
						InputStream = inputStream,
						MaxDimension = 16
					});
				}
			}
			catch (Exception e) {
				Debug.WriteLine(e.Message);
			}

			return string.Empty;
		}

		/// <summary>
		/// Searches a post for references to other users
		/// </summary>
		public void FindMentionedUsers(InputModels.ProcessedMessageInput processedMessageInput) {
			var regexUsers = new Regex(@"@(\S+)");

			var matches = 0;

			foreach (Match regexMatch in regexUsers.Matches(processedMessageInput.DisplayBody)) {
				matches++;

				// DoS prevention
				if (matches > 10) {
					break;
				}

				var matchedTag = regexMatch.Groups[1].Value;

				var user = AccountRepository.FirstOrDefault(u => u.DisplayName.ToLower() == matchedTag.ToLower());

				// try to guess what they meant
				if (user is null) {
					user = AccountRepository.FirstOrDefault(u => u.UserName.ToLower().Contains(matchedTag.ToLower()));
				}

				if (user != null) {
					if (user.Id != UserContext.ApplicationUser.Id) {
						processedMessageInput.MentionedUsers.Add(user.Id);
					}

					// Eventually link to user profiles
					// returnObject.ProcessedBody = Regex.Replace(returnObject.ProcessedBody, @"@" + regexMatch.Groups[1].Value, "<a href='/Profile/Details/" + user.UserId + "' class='user'>" + user.DisplayName + "</span>");
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

		public DataModels.Message CreateMessageRecord(InputModels.ProcessedMessageInput processedMessage, DataModels.Message replyRecord) {
			var parentId = 0;
			var replyId = 0;

			DataModels.Message parentMessage = null;

			if (replyRecord != null) {
				if (replyRecord.ParentId == 0) {
					parentId = replyRecord.Id;
					replyId = 0;

					parentMessage = replyRecord;
				}
				else {
					parentId = replyRecord.ParentId;
					replyId = replyRecord.Id;

					parentMessage = DbContext.Messages.First(item => item.Id == replyRecord.ParentId);
				}
			}

			var currentTime = DateTime.Now;

			var record = new DataModels.Message {
				OriginalBody = processedMessage.OriginalBody,
				DisplayBody = processedMessage.DisplayBody,
				ShortPreview = processedMessage.ShortPreview,
				LongPreview = processedMessage.LongPreview,
				Cards = processedMessage.Cards,

				TimePosted = currentTime,
				TimeEdited = currentTime,
				LastReplyPosted = currentTime,

				PostedById = UserContext.ApplicationUser.Id,
				EditedById = UserContext.ApplicationUser.Id,
				LastReplyById = UserContext.ApplicationUser.Id,

				ParentId = parentId,
				ReplyId = replyId,

				Processed = true
			};

			DbContext.Messages.Add(record);
			DbContext.SaveChanges();

			if (replyId > 0) {
				replyRecord.LastReplyId = record.Id;
				replyRecord.LastReplyById = UserContext.ApplicationUser.Id;
				replyRecord.LastReplyPosted = currentTime;

				DbContext.Update(replyRecord);

				if (replyRecord.PostedById != UserContext.ApplicationUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = record.Id,
						UserId = replyRecord.PostedById,
						TargetUserId = UserContext.ApplicationUser.Id,
						Time = DateTime.Now,
						Type = Enums.ENotificationType.Quote,
						Unread = true,
					};

					DbContext.Notifications.Add(notification);
				}
			}

			if (parentMessage != null && parentId != replyId) {
				parentMessage.ReplyCount++;
				parentMessage.LastReplyId = record.Id;
				parentMessage.LastReplyById = UserContext.ApplicationUser.Id;
				parentMessage.LastReplyPosted = currentTime;

				DbContext.Update(parentMessage);

				if (parentMessage.PostedById != UserContext.ApplicationUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = record.Id,
						UserId = parentMessage.PostedById,
						TargetUserId = UserContext.ApplicationUser.Id,
						Time = DateTime.Now,
						Type = Enums.ENotificationType.Reply,
						Unread = true,
					};

					DbContext.Notifications.Add(notification);
				}
			}

			NotifyMentionedUsers(processedMessage.MentionedUsers, record.Id);

			var topicId = parentId == 0 ? record.Id : parentId;
			UpdateTopicParticipation(topicId, UserContext.ApplicationUser.Id, DateTime.Now);

			DbContext.SaveChanges();

			return record;
		}

		public void UpdateMessageRecord(InputModels.ProcessedMessageInput message, DataModels.Message record) {
			record.OriginalBody = message.OriginalBody;
			record.DisplayBody = message.DisplayBody;
			record.ShortPreview = message.ShortPreview;
			record.LongPreview = message.LongPreview;
			record.Cards = message.Cards;
			record.TimeEdited = DateTime.Now;
			record.EditedById = UserContext.ApplicationUser.Id;
			record.Processed = true;

			DbContext.Update(record);

			DbContext.SaveChanges();
		}

		public void UpdateTopicParticipation(int topicId, string userId, DateTime time) {
			var participation = DbContext.Participants.FirstOrDefault(r => r.MessageId == topicId && r.UserId == userId);

			if (participation is null) {
				DbContext.Participants.Add(new DataModels.Participant {
					MessageId = topicId,
					UserId = userId,
					Time = time
				});
			}
			else {
				participation.Time = time;
				DbContext.Update(participation);
			}
		}

		public void NotifyMentionedUsers(List<string> mentionedUsers, int messageId) {
			foreach (var user in mentionedUsers) {
				var notification = new DataModels.Notification {
					MessageId = messageId,
					TargetUserId = UserContext.ApplicationUser.Id,
					UserId = user,
					Time = DateTime.Now,
					Type = Enums.ENotificationType.Mention,
					Unread = true
				};

				DbContext.Notifications.Add(notification);
			}
		}

		public int RecountReplies() {
			var query = from message in DbContext.Messages
						where message.ParentId == 0
						select message.Id;

			var recordCount = query.Count();

			var take = SettingsRepository.TopicsPerPage(true);
			var totalSteps = (int)Math.Ceiling(1D * recordCount / take);

			return totalSteps;
		}

		public void RecountRepliesContinue(InputModels.Continue input) {
			input.ThrowIfNull(nameof(input));

			var parentMessageQuery = from message in DbContext.Messages
									 where message.ParentId == 0
									 orderby message.Id descending
									 select message;

			var take = SettingsRepository.TopicsPerPage(true);
			var skip = input.CurrentStep * take;

			var parents = parentMessageQuery.Skip(skip).Take(take).ToList();

			foreach (var parent in parents) {
				RecountRepliesForTopic(parent);
			}
		}

		public void RecountRepliesForTopic(DataModels.Message parentMessage) {
			var messagesQuery = from message in DbContext.Messages
								where message.ParentId == parentMessage.Id
								select message;

			var messages = messagesQuery.ToList();

			var updated = false;

			var replies = messages.Count();

			if (parentMessage.ReplyCount != replies) {
				parentMessage.ReplyCount = replies;
				updated = true;
			}

			var lastReply = messages.LastOrDefault();

			if (lastReply is null) {
				parentMessage.LastReplyId = 0;
				parentMessage.LastReplyPosted = parentMessage.TimePosted;
				parentMessage.LastReplyById = parentMessage.PostedById;
				updated = true;
			}
			else if (parentMessage.LastReplyId != lastReply.Id) {
				parentMessage.LastReplyId = lastReply.Id;
				parentMessage.LastReplyPosted = lastReply.TimePosted;
				parentMessage.LastReplyById = lastReply.PostedById;
				updated = true;
			}

			if (updated) {
				DbContext.Update(parentMessage);
				DbContext.SaveChanges();
			}
		}

		public int RebuildParticipants() {
			var query = from message in DbContext.Messages
						where message.ParentId == 0
						select message.Id;

			var recordCount = query.Count();

			var take = SettingsRepository.TopicsPerPage(true);
			var totalSteps = (int)Math.Ceiling(1D * recordCount / take);

			return totalSteps;
		}

		public void RebuildParticipantsContinue(InputModels.Continue input) {
			input.ThrowIfNull(nameof(input));

			var parentMessageQuery = from message in DbContext.Messages
									 where message.ParentId == 0
									 orderby message.Id descending
									 select message;

			var take = SettingsRepository.TopicsPerPage(true);
			var skip = input.CurrentStep * take;

			var parents = parentMessageQuery.Skip(skip).Take(take).ToList();

			foreach (var parent in parents) {
				RebuildParticipantsForTopic(parent.Id);
			}
		}

		public void RebuildParticipantsForTopic(int topicId) {
			var messagesQuery = from message in DbContext.Messages
								where message.Id == topicId || message.ParentId == topicId
								select message;

			var messages = messagesQuery.ToList();

			var newParticipants = new List<DataModels.Participant>();

			foreach (var message in messages) {
				if (!newParticipants.Any(item => item.UserId == message.PostedById)) {
					newParticipants.Add(new DataModels.Participant {
						MessageId = topicId,
						UserId = message.PostedById,
						Time = message.TimePosted
					});
				}
			}

			var oldParticipants = DbContext.Participants.Where(r => r.MessageId == topicId).ToList();

			if (oldParticipants.Count() != newParticipants.Count()) {
				DbContext.RemoveRange(oldParticipants);
				DbContext.SaveChanges();

				DbContext.Participants.AddRange(newParticipants);
				DbContext.SaveChanges();
			}
		}

		public int ReprocessMessages() => ProcessMessages(true);
		public int ProcessMessages(bool force = false) {
			var records = from message in DbContext.Messages
						  where force || !message.Processed
						  select message.Id;

			var recordCount = records.Count();

			var take = SettingsRepository.MessagesPerPage(true);
			var totalSteps = (int)Math.Ceiling(1D * recordCount / take);

			return totalSteps;
		}

		public async Task ReprocessMessagesContinue(InputModels.Continue input) => await ProcessMessagesContinue(input, true);
		public async Task ProcessMessagesContinue(InputModels.Continue input, bool force = false) {
			input.ThrowIfNull(nameof(input));

			var messageQuery = from message in DbContext.Messages
							   where force || !message.Processed
							   orderby message.Id descending
							   select message;

			var take = SettingsRepository.MessagesPerPage(true);
			var skip = take * input.CurrentStep;

			var messages = messageQuery.Skip(skip).Take(take).ToList();

			foreach (var message in messages) {
				var serviceResponse = new ServiceModels.ServiceResponse();

				var processedMessage = await ProcessMessageInput(serviceResponse, message.OriginalBody);

				if (serviceResponse.Success) {
					message.OriginalBody = processedMessage.OriginalBody;
					message.DisplayBody = processedMessage.DisplayBody;
					message.ShortPreview = processedMessage.ShortPreview;
					message.LongPreview = processedMessage.LongPreview;
					message.Cards = processedMessage.Cards;
					message.Processed = true;

					DbContext.Update(message);
				}
			}

			DbContext.SaveChanges();
		}
	}
}