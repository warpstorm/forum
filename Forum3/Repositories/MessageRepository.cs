using CodeKicker.BBCode;
using Forum3.Contexts;
using Forum3.Extensions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class MessageRepository {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		UserRepository UserRepository { get; }
		IUrlHelper UrlHelper { get; }

		public MessageRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			UserRepository userRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			UserRepository = userRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public int GetPageNumber(int messageId, List<int> messageIds) {
			var index = (double)messageIds.FindIndex(id => id == messageId);
			index++;

			var messagesPerPage = SettingsRepository.MessagesPerPage();
			return Convert.ToInt32(Math.Ceiling(index / messagesPerPage));
		}

		public void MigrateMessageRecord(DataModels.Message record, bool force = false) {
			if (record.Processed && !force)
				return;

			if (record.LegacyId == 0) {
				record.Processed = true;
				DbContext.Update(record);
				return;
			}

			var parentTask = DbContext.Messages.FirstOrDefaultAsync(m => record.LegacyParentId != 0 && m.LegacyId == record.LegacyParentId);
			var replyTask = DbContext.Messages.FirstOrDefaultAsync(m => record.LegacyReplyId != 0 && m.LegacyId == record.LegacyReplyId);

			Task.WaitAll(parentTask, replyTask);

			var message = ProcessMessageInput(new ServiceModels.ServiceResponse(), record.OriginalBody);

			record.OriginalBody = message.OriginalBody;
			record.DisplayBody = message.DisplayBody;
			record.ShortPreview = message.ShortPreview;
			record.LongPreview = message.LongPreview;
			record.Cards = message.Cards;
			record.Processed = true;

			record.ReplyId = replyTask.Result?.Id ?? 0;
			record.PostedById = UserRepository.All.FirstOrDefault(u => u.LegacyId == record.LegacyPostedById)?.Id ?? string.Empty;
			record.EditedById = UserRepository.All.FirstOrDefault(u => u.LegacyId == record.LegacyEditedById)?.Id ?? string.Empty;

			var parent = parentTask.Result;

			if (parent is null) {
				UpdateTopicParticipation(record.Id, record.PostedById, record.TimePosted);
			}
			else {
				if (record.Id != parent.Id)
					record.ParentId = parent.Id;

				DbContext.Update(record);

				if (record.Id != parent.Id) {
					parent.LastReplyId = record.Id;
					parent.LastReplyById = record.PostedById;
					parent.LastReplyPosted = record.TimePosted;

					DbContext.Update(parent);
				}

				UpdateTopicParticipation(parent.Id, record.PostedById, record.TimePosted);
			}
		}

		public ServiceModels.ServiceResponse CreateTopic(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.BoardId is null)
				serviceResponse.Error(nameof(input.BoardId), $"Board ID is required");

			var boardId = Convert.ToInt32(input.BoardId);
			var boardRecord = DbContext.Boards.SingleOrDefault(b => b.Id == boardId);

			if (boardRecord is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{boardId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var processedMessage = ProcessMessageInput(serviceResponse, input.Body);

			if (!serviceResponse.Success)
				return serviceResponse;

			var record = CreateMessageRecord(processedMessage, null);

			DbContext.MessageBoards.Add(new DataModels.MessageBoard {
				MessageId = record.Id,
				BoardId = boardRecord.Id,
				TimeAdded = DateTime.Now,
				UserId = UserContext.ApplicationUser.Id
			});

			boardRecord.LastMessageId = record.Id;

			DbContext.Update(boardRecord);
			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> CreateReply(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Id == 0)
				throw new Exception($"No record ID specified.");

			var replyRecord = DbContext.Messages.FirstOrDefault(m => m.Id == input.Id);
			var processedMessage = ProcessMessageInput(serviceResponse, input.Body);

			if (replyRecord is null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.Id}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var record = CreateMessageRecord(processedMessage, replyRecord);

			var boardRecords = await (from message in DbContext.Messages
									  join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
									  join board in DbContext.Boards on messageBoard.BoardId equals board.Id
									  where message.Id == record.ParentId
									  select board).ToListAsync();

			foreach (var boardRecord in boardRecords) {
				boardRecord.LastMessageId = record.ParentId;
				DbContext.Update(boardRecord);
			}

			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
			return serviceResponse;
		}

		public ServiceModels.ServiceResponse EditMessage(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Id == 0)
				throw new Exception($"No record ID specified.");

			var record = DbContext.Messages.FirstOrDefault(m => m.Id == input.Id);
			var processedMessage = ProcessMessageInput(serviceResponse, input.Body);

			if (serviceResponse.Success) {
				serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
				UpdateMessageRecord(processedMessage, record);
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> DeleteMessage(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = await DbContext.Messages.SingleAsync(m => m.Id == messageId);

			if (record is null)
				serviceResponse.Error(string.Empty, $@"No record was found with the id '{messageId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			if (record.ParentId != 0) {
				serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.ParentId);

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
			else
				serviceResponse.RedirectPath = UrlHelper.TopicIndex();

			var topicReplies = await DbContext.Messages.Where(m => m.ParentId == messageId).ToListAsync();

			foreach (var reply in topicReplies) {
				var replyThoughts = await DbContext.MessageThoughts.Where(mt => mt.MessageId == reply.Id).ToListAsync();

				foreach (var replyThought in replyThoughts)
					DbContext.MessageThoughts.Remove(replyThought);

				DbContext.Messages.Remove(reply);
			}

			var messageBoards = await DbContext.MessageBoards.Where(m => m.MessageId == record.Id).ToListAsync();

			foreach (var messageBoard in messageBoards)
				DbContext.MessageBoards.Remove(messageBoard);

			var messageThoughts = await DbContext.MessageThoughts.Where(mt => mt.MessageId == record.Id).ToListAsync();

			foreach (var messageThought in messageThoughts)
				DbContext.MessageThoughts.Remove(messageThought);

			DbContext.Messages.Remove(record);

			DbContext.SaveChanges();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> AddThought(InputModels.ThoughtInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = DbContext.Messages.Find(input.MessageId);

			if (messageRecord is null)
				serviceResponse.Error(string.Empty, $@"No message was found with the id '{input.MessageId}'");

			var smileyRecord = await DbContext.Smileys.FindAsync(input.SmileyId);

			if (messageRecord is null)
				serviceResponse.Error(string.Empty, $@"No smiley was found with the id '{input.SmileyId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

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
			else
				DbContext.MessageThoughts.Remove(existingRecord);

			DbContext.SaveChanges();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(input.MessageId);
			return serviceResponse;
		}

		public InputModels.ProcessedMessageInput ProcessMessageInput(ServiceModels.ServiceResponse serviceResponse, string messageBody) {
			var processedMessage = PreProcessMessageInput(messageBody);
			PreProcessSmileys(processedMessage);
			ParseBBC(processedMessage);
			ProcessSmileys(processedMessage);
			ProcessMessageBodyUrls(processedMessage);
			FindMentionedUsers(processedMessage);
			PostProcessMessageInput(processedMessage);

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
		public void ParseBBC(InputModels.ProcessedMessageInput processedMessageInput) {
			var displayBody = processedMessageInput.DisplayBody;

			var parser = new BBCodeParser(new[] {
				new BBTag("b", @"<span class=""bbc-bold"">", "</span>"),
				new BBTag("s", @"<span class=""bbc-strike"">", "</span>"),
				new BBTag("i", @"<span class=""bbc-italic"">", "</span>"),
				new BBTag("u", @"<span class=""bbc-underline"">", "</span>"),
				new BBTag("code", @"<div class=""bbc-code"">", "</div>"),
				new BBTag("img", @"<img class=""bbc-image"" src=""${content}"" />", "", false, true),
				new BBTag("quote", @"<blockquote class=""bbc-quote"">", "</blockquote>"),
				new BBTag("ul", @"<ul class=""bbc-list"">", "</ul>"),
				new BBTag("ol", @"<ol class=""bbc-list"">", "</ul>"),
				new BBTag("li", @"<li class=""bbc-list-item"">", "</li>"),
				new BBTag("url", @"<a class=""bbc-anchor"" href=""${href}"" target=""_blank"">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
				new BBTag("color", @"<span style=""color: ${color};"">", "</span>", new BBAttribute("color", ""), new BBAttribute("color", "color")),
			});

			processedMessageInput.DisplayBody = parser.ToHtml(displayBody);
		}

		/// <summary>
		/// Attempt to replace URLs in the message body with something better
		/// </summary>
		public void ProcessMessageBodyUrls(InputModels.ProcessedMessageInput processedMessageInput) {
			var displayBody = processedMessageInput.DisplayBody;

			var regexUrl = new Regex("(^| )((https?\\://){1}\\S+)", RegexOptions.Compiled | RegexOptions.Multiline);

			var matches = 0;

			foreach (Match regexMatch in regexUrl.Matches(displayBody)) {
				matches++;

				// DoS prevention
				if (matches > 10)
					break;

				var siteUrl = regexMatch.Groups[2].Value;

				if (!string.IsNullOrEmpty(siteUrl)) {
					var remoteUrlReplacement = GetRemoteUrlReplacement(siteUrl);

					displayBody = remoteUrlReplacement.Regex.Replace(displayBody, remoteUrlReplacement.ReplacementText, 1);

					processedMessageInput.Cards += remoteUrlReplacement.Card;
				}
			}

			processedMessageInput.DisplayBody = displayBody;
		}

		public void PreProcessSmileys(InputModels.ProcessedMessageInput processedMessageInput) {
			for (var i = 0; i < SmileyRepository.All.Count(); i++) {
				var pattern = @"(^|[\r\n\s])" + Regex.Escape(SmileyRepository.All[i].Code) + @"(?=$|[\r\n\s])";
				var replacement = $"$1SMILEY_{i}_INDEX";
				processedMessageInput.DisplayBody = Regex.Replace(processedMessageInput.DisplayBody, pattern, replacement, RegexOptions.Singleline);
			}
		}

		public void ProcessSmileys(InputModels.ProcessedMessageInput processedMessageInput) {
			for (var i = 0; i < SmileyRepository.All.Count(); i++) {
				var pattern = $@"SMILEY_{i}_INDEX";
				var replacement = "<img src='" + SmileyRepository.All[i].Path + "' />";
				processedMessageInput.DisplayBody = Regex.Replace(processedMessageInput.DisplayBody, pattern, replacement);
			}
		}

		/// <summary>
		/// Attempt to replace the ugly URL with a human readable title.
		/// </summary>
		public ServiceModels.RemoteUrlReplacement GetRemoteUrlReplacement(string remoteUrl) {
			var remotePageDetails = GetRemotePageDetails(remoteUrl);
			remotePageDetails.Title = remotePageDetails.Title.Replace("$", "&#36;");

			const string youtubePattern = @"(?:https?:\/\/)?(?:www\.)?(?:(?:(?:youtube.com\/watch\?[^?]*v=|youtu.be\/)([\w\-]+))(?:[^\s?]+)?)";
			const string youtubeIframePartial = "<iframe type='text/html' title='YouTube video player' class='youtubePlayer' src='https://www.youtube.com/embed/{0}' frameborder='0' allowfullscreen='1'></iframe>";
			const string embeddedVideoPartial = "<video autoplay loop><source src='{0}.webm' type='video/webm' /><source src='{0}.mp4' type='video/mp4' /></video>";

			var regexYoutube = new Regex(youtubePattern);
			var regexEmbeddedVideo = new Regex(@"(^| )((https?\://){1}\S+)(.gifv|.webm|.mp4)", RegexOptions.Compiled | RegexOptions.Multiline);
			var regexUrl = new Regex(@"(^| )((https?\://){1}\S+)", RegexOptions.Compiled | RegexOptions.Multiline);

			// check first if the link is a youtube vid
			if (regexYoutube.Match(remoteUrl).Success) {
				var youtubeVideoId = regexYoutube.Match(remoteUrl).Groups[1].Value;
				var youtubeIframeClosed = string.Format(youtubeIframePartial, youtubeVideoId);

				return new ServiceModels.RemoteUrlReplacement {
					Regex = regexYoutube,
					ReplacementText = "<a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
					Card = $@"<div class=""embedded-video"">{youtubeIframeClosed}</div>"
				};
			}
			// or is it an embedded video link
			else if (regexEmbeddedVideo.Match(remoteUrl).Success) {
				var embeddedVideoId = regexEmbeddedVideo.Match(remoteUrl).Groups[2].Value;
				var embeddedVideoTag = string.Format(embeddedVideoPartial, embeddedVideoId);

				return new ServiceModels.RemoteUrlReplacement {
					Regex = regexEmbeddedVideo,
					ReplacementText = " <a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
					Card = $@"<div class=""embedded-video"">{embeddedVideoTag}</div>"
				};
			}

			// replace the URL with the HTML
			return new ServiceModels.RemoteUrlReplacement {
				Regex = regexUrl,
				ReplacementText = "$1<a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
				Card = remotePageDetails.Card ?? string.Empty
			};
		}

		/// <summary>
		/// I really should make this async. Load a remote page by URL and attempt to get details about it.
		/// </summary>
		public ServiceModels.RemotePageDetails GetRemotePageDetails(string remoteUrl) {
			var returnResult = new ServiceModels.RemotePageDetails {
				Title = remoteUrl
			};

			var siteWithoutHash = remoteUrl.Split('#')[0];

			HtmlDocument document = null;

			var client = new HtmlWeb() {
				UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36"
			};

			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			client.PreRequest += request => {
				request.ServicePoint.Expect100Continue = false;
				request.AllowAutoRedirect = true;
				request.MaximumAutomaticRedirections = 3;
				request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
				request.Timeout = 5000;
				request.CookieContainer = new CookieContainer();

				return true;
			};

			try {
				var documentTask = client.LoadFromWebAsync(siteWithoutHash);

				Task.WaitAll(new Task[] { documentTask });

				document = documentTask.Result;
			}
			// System.InvalidOperationException: 'The character set provided in ContentType is invalid. Cannot read content as string using an invalid character set.'
			catch (InvalidOperationException) { }
			catch (Exception e) when (e.Message == "Error downloading html") { }

			if (document is null)
				return returnResult;

			var titleTag = document.DocumentNode.SelectSingleNode(@"//title");

			if (titleTag != null && !string.IsNullOrEmpty(titleTag.InnerText.Trim()))
				returnResult.Title = titleTag.InnerText.Trim();

			// try to find the opengraph title
			var ogTitle = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:title']");
			var ogSiteName = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:site_name']");
			var ogImage = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:image']");
			var ogDescription = document.DocumentNode.SelectSingleNode(@"//meta[@property='og:description']");

			if (ogTitle != null && ogTitle.Attributes["content"] != null && !string.IsNullOrEmpty(ogTitle.Attributes["content"].Value.Trim())) {
				returnResult.Title = ogTitle.Attributes["content"].Value.Trim();

				if (ogDescription != null && ogDescription.Attributes["content"] != null && !string.IsNullOrEmpty(ogDescription.Attributes["content"].Value.Trim())) {
					returnResult.Card += "<blockquote class='card pointer hover-highlight' clickable-link-parent>";

					if (ogImage != null && ogImage.Attributes["content"] != null && !string.IsNullOrEmpty(ogImage.Attributes["content"].Value.Trim()))
						returnResult.Card += "<div class='card-image'><img src='" + ogImage.Attributes["content"].Value.Trim() + "' /></div>";

					returnResult.Card += "<div>";
					returnResult.Card += "<p class='card-title'><a target='_blank' href='" + remoteUrl + "'>" + returnResult.Title + "</a></p>";

					var decodedDescription = WebUtility.HtmlDecode(ogDescription.Attributes["content"].Value.Trim());

					returnResult.Card += "<p class='card-description'>" + decodedDescription + "</p>";

					if (ogSiteName != null && ogSiteName.Attributes["content"] != null && !string.IsNullOrEmpty(ogSiteName.Attributes["content"].Value.Trim()))
						returnResult.Card += "<p class='card-link'><a target='_blank' href='" + remoteUrl + "'>[" + ogSiteName.Attributes["content"].Value.Trim() + "]</a></p>";
					else
						returnResult.Card += "<p class='card-link'><a target='_blank' href='" + remoteUrl + "'>[Direct Link]</a></p>";

					returnResult.Card += "</div><br class='clear' /></blockquote>";
				}
			}

			return returnResult;
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
				if (matches > 10)
					break;

				var matchedTag = regexMatch.Groups[1].Value;

				var user = UserRepository.All.SingleOrDefault(u => u.DisplayName.ToLower() == matchedTag.ToLower());

				// try to guess what they meant
				if (user is null)
					user = UserRepository.All.FirstOrDefault(u => u.UserName.ToLower().Contains(matchedTag.ToLower()));

				if (user != null) {
					if (user.Id != UserContext.ApplicationUser.Id)
						processedMessageInput.MentionedUsers.Add(user.Id);

					// Eventually link to user profiles
					// returnObject.ProcessedBody = Regex.Replace(returnObject.ProcessedBody, @"@" + regexMatch.Groups[1].Value, "<a href='/Profile/Details/" + user.UserId + "' class='user'>" + user.DisplayName + "</span>");
				}
			}
		}

		/// <summary>
		/// Minor post processing
		/// </summary>
		public void PostProcessMessageInput(InputModels.ProcessedMessageInput processedMessageInput) {
			processedMessageInput.DisplayBody = processedMessageInput.DisplayBody.Trim();
			processedMessageInput.ShortPreview = GetMessagePreview(processedMessageInput.DisplayBody, 100);
			processedMessageInput.LongPreview = GetMessagePreview(processedMessageInput.DisplayBody, 500, true);
		}

		/// <summary>
		/// Gets a reduced version of the message without HTML
		/// </summary>
		public string GetMessagePreview(string messageBody, int previewLength, bool multiline = false) {
			// strip out quotes
			var preview = Regex.Replace(messageBody, @"(<blockquote.*?>.+?</blockquote>\n*?)", string.Empty, RegexOptions.Compiled);

			// strip out tags
			preview = Regex.Replace(preview, @"(<.+?>|\[.+?\])", string.Empty, RegexOptions.Compiled);

			if (!multiline)
				preview = Regex.Match(preview, @"^(.+)?\n*", RegexOptions.Compiled).Groups[1].Value;

			if (preview.Length > previewLength) {
				var matches = Regex.Match(preview, @"^(.{" + (previewLength - 1) + "})", RegexOptions.Compiled);
				preview = matches.Groups[1].Value + "…";
			}
			else if (preview.Length <= 0)
				preview = "No text";

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

					parentMessage = DbContext.Messages.Find(replyRecord.ParentId);

					if (parentMessage is null)
						throw new Exception($"Orphan message found with ID {replyRecord.Id}. Unable to load parent with ID {replyRecord.ParentId}.");
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

		public ViewModels.Delay RebuildThreadsStart() {
			var query = from message in DbContext.Messages
						where message.LegacyParentId == 0
						where message.ParentId == 0
						select message.Id;

			var recordCount = query.Count();

			var take = SettingsRepository.TopicsPerPage();
			var totalSteps = (int)Math.Ceiling(1D * recordCount / take);

			return RebuildThreadsViewModel(new InputModels.Continue {
				Stage = nameof(RebuildThreadsContinue),
				CurrentStep = -1,
				TotalSteps = totalSteps
			});
		}

		public ViewModels.Delay RebuildThreadsContinue(InputModels.Continue input) {
			input.ThrowIfNull(nameof(input));

			var parentMessageQuery = from message in DbContext.Messages
									 where message.LegacyParentId == 0
									 where message.ParentId == 0
									 orderby message.Id descending
									 select message;

			var take = SettingsRepository.TopicsPerPage();
			var skip = input.CurrentStep * take;

			var parents = parentMessageQuery.Skip(skip).Take(take).ToList();

			foreach (var parent in parents) {
				var messagesQuery = from message in DbContext.Messages
									where message.ParentId == parent.Id || (parent.LegacyId != 0 && message.LegacyParentId == parent.LegacyId)
									select message;

				var messages = messagesQuery.ToList();

				var pages = (int) Math.Ceiling(1D * messages.Count() / take);

				for (var i = 0; i < pages; i++) {
					foreach (var message in messages.Skip(i * take).Take(take))
						RebuildMessageRelationships(parent, message, messages);

					DbContext.SaveChanges();
				}

				RecountReplies(parent, messages);
				RebuildParticipants(parent, messages);
			}

			return RebuildThreadsViewModel(input);
		}

		public ViewModels.Delay RebuildThreadsViewModel(InputModels.Continue input) {
			var viewModel = new ViewModels.Delay {
				ActionName = "Rebuilding Threads",
				ActionNote = "Relinking child posts to their parent posts.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Controllers.Messages.Admin), nameof(Controllers.Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Controllers.Messages.RebuildThreads), nameof(Controllers.Messages), input);
			}

			return viewModel;
		}

		public void RebuildMessageRelationships(DataModels.Message parent, DataModels.Message message, List<DataModels.Message> messages) {
			if (message.ParentId == 0 && message.LegacyParentId != 0)
				message.ParentId = parent.Id;

			if (message.ReplyId == 0 && message.LegacyReplyId != 0) {
				DataModels.Message reply = null;

				if (message.LegacyReplyId == parent.LegacyId)
					reply = parent;
				else
					reply = messages.First(item => item.LegacyId == message.LegacyReplyId);

				message.ReplyId = reply.Id;
			}

			if (string.IsNullOrEmpty(message.PostedById)) {
				var user = UserRepository.All.FirstOrDefault(item => item.LegacyId == message.LegacyPostedById);
				message.PostedById = user?.Id ?? string.Empty;
			}

			if (string.IsNullOrEmpty(message.EditedById)) {
				var user = UserRepository.All.FirstOrDefault(item => item.LegacyId == message.LegacyEditedById);
				message.EditedById = user?.Id ?? string.Empty;
			}
		}

		public void RecountReplies(DataModels.Message parentMessage, List<DataModels.Message> messages) {
			var updated = false;

			var replies = messages.Count();

			if (parentMessage.ReplyCount != replies) {
				parentMessage.ReplyCount = replies;
				updated = true;
			}

			var lastReply = messages.LastOrDefault();

			if (lastReply != null && parentMessage.LastReplyId != lastReply.Id) {
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

		public void RebuildParticipants(DataModels.Message parentMessage, List<DataModels.Message> messages) {
			var newParticipants = new List<DataModels.Participant> {
				new DataModels.Participant {
					MessageId = parentMessage.Id,
					UserId = parentMessage.PostedById,
					Time = parentMessage.TimePosted
				}
			};

			foreach (var message in messages) {
				if (!newParticipants.Any(item => item.UserId == message.PostedById)) {
					newParticipants.Add(new DataModels.Participant {
						MessageId = parentMessage.Id,
						UserId = message.PostedById,
						Time = message.TimePosted
					});
				}
			}

			var oldParticipants = DbContext.Participants.Where(r => r.MessageId == parentMessage.Id).ToList();

			if (oldParticipants.Count() != newParticipants.Count()) {
				DbContext.RemoveRange(oldParticipants);
				DbContext.SaveChanges();

				DbContext.Participants.AddRange(newParticipants);
				DbContext.SaveChanges();
			}
		}

		public ViewModels.Delay ReprocessMessagesStart() {
			var recordCount = DbContext.Messages.Count();

			var take = SettingsRepository.MessagesPerPage();
			var totalSteps = (int)Math.Ceiling(1D * recordCount / take);

			return ReprocessMessagesViewModel(new InputModels.Continue {
				Stage = nameof(ReprocessMessagesContinue),
				CurrentStep = -1,
				TotalSteps = totalSteps
			});
		}

		public ViewModels.Delay ReprocessMessagesContinue(InputModels.Continue input) {
			input.ThrowIfNull(nameof(input));

			var messageQuery = from message in DbContext.Messages
							   orderby message.Id descending
							   select message;

			var take = SettingsRepository.MessagesPerPage();
			var skip = take * input.CurrentStep;

			var messages = messageQuery.Skip(skip).Take(take).ToList();

			// This is discarded.
			var serviceResponse = new ServiceModels.ServiceResponse();

			foreach (var message in messages) {
				var processedMessage = ProcessMessageInput(serviceResponse, message.OriginalBody);

				message.OriginalBody = processedMessage.OriginalBody;
				message.DisplayBody = processedMessage.DisplayBody;
				message.ShortPreview = processedMessage.ShortPreview;
				message.LongPreview = processedMessage.LongPreview;
				message.Cards = processedMessage.Cards;
				message.Processed = true;

				DbContext.Update(message);
			}

			DbContext.SaveChanges();

			return ReprocessMessagesViewModel(input);
		}

		public ViewModels.Delay ReprocessMessagesViewModel(InputModels.Continue input) {
			var viewModel = new ViewModels.Delay {
				ActionName = "Running topic post-migration",
				ActionNote = "Build threads, counting replies, and processing message text.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Controllers.Messages.Admin), nameof(Controllers.Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Controllers.Messages.ReprocessMessages), nameof(Controllers.Messages), input);
			}

			return viewModel;
		}
	}
}