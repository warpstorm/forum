using CodeKicker.BBCode;
using Forum3.Controllers;
using Forum3.Helpers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum3.Services.Controller {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels.Messages;

	public class MessageService {
		DataModels.ApplicationDbContext DbContext { get; }
		ServiceModels.ContextUser ContextUser { get; }
		IUrlHelper UrlHelper { get; }

		public MessageService(
			DataModels.ApplicationDbContext dbContext,
			ContextUserFactory contextUserFactory,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			ContextUser = contextUserFactory.GetContextUser();
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<ViewModels.CreateTopicPage> CreatePage(int boardId = 0) {
			var board = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == boardId);

			if (board == null)
				throw new Exception($"A record does not exist with ID '{boardId}'");

			var viewModel = new ViewModels.CreateTopicPage {
				BoardId = boardId
			};

			return viewModel;
		}

		public async Task<ViewModels.EditMessagePage> EditPage(int messageId) {
			var record = await DbContext.Messages.SingleOrDefaultAsync(m => m.Id == messageId);

			if (record == null)
				throw new Exception($"A record does not exist with ID '{messageId}'");

			var viewModel = new ViewModels.EditMessagePage {
				Id = messageId,
				Body = record.OriginalBody
			};

			return viewModel;
		}

		public async Task<ViewModels.MigrateMessagePage> MigratePage(int messageId) {
			var viewModel = new ViewModels.MigrateMessagePage();

			var record = await DbContext.Messages.FindAsync(messageId);

			if (record == null)
				throw new Exception($@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0)
				viewModel.RedirectPath = UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = record.ParentId });
			else
				viewModel.RedirectPath = UrlHelper.Action(nameof(Messages.FinishMigration), nameof(Messages), new { id = record.Id });

			return viewModel;
		}

		public async Task<ServiceModels.ServiceResponse> CreateTopic(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.BoardId == null)
				serviceResponse.Error(nameof(input.BoardId), $"Board ID is required");

			var boardId = Convert.ToInt32(input.BoardId);
			var boardRecord = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == boardId);

			if (boardRecord == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{boardId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var processedMessage = await ProcessMessageInput(serviceResponse, input.Body);

			if (!serviceResponse.Success)
				return serviceResponse;

			var record = await CreateMessageRecord(processedMessage, null);

			DbContext.MessageBoards.Add(new DataModels.MessageBoard {
				MessageId = record.Id,
				BoardId = boardRecord.Id,
				TimeAdded = DateTime.Now,
				UserId = ContextUser.ApplicationUser.Id
			});

			boardRecord.LastMessageId = record.Id;

			DbContext.Update(boardRecord);
			await DbContext.SaveChangesAsync();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> CreateReply(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Id == 0)
				throw new Exception($"No record ID specified.");

			var processedMessageTask = ProcessMessageInput(serviceResponse, input.Body);
			var replyRecordTask = DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			await Task.WhenAll(replyRecordTask, processedMessageTask);

			var replyRecord = await replyRecordTask;
			var processedMessage = await processedMessageTask;

			if (replyRecord == null)
				serviceResponse.Error(string.Empty, $"A record does not exist with ID '{input.Id}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var record = await CreateMessageRecord(processedMessage, replyRecord);

			var boardRecord = await (from message in DbContext.Messages
								join messageBoard in DbContext.MessageBoards on message.Id equals messageBoard.MessageId
								join board in DbContext.Boards on messageBoard.BoardId equals board.Id
								where message.Id == record.ParentId
								select board).FirstOrDefaultAsync();

			if (boardRecord != null) {
				boardRecord.LastMessageId = record.Id;

				DbContext.Update(boardRecord);
				await DbContext.SaveChangesAsync();
			}

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> EditMessage(InputModels.MessageInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Id == 0)
				throw new Exception($"No record ID specified.");

			var processedMessageTask = ProcessMessageInput(serviceResponse, input.Body);
			var recordTask = DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			await Task.WhenAll(recordTask, processedMessageTask);

			var record = await recordTask;
			var processedMessage = await processedMessageTask;

			if (serviceResponse.Success) {
				serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);
				await UpdateMessageRecord(processedMessage, record);
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> DeleteMessage(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = await DbContext.Messages.SingleAsync(m => m.Id == messageId);

			if (record == null)
				serviceResponse.Error(string.Empty, $@"No record was found with the id '{messageId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			if (record.ParentId != 0) {
				serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.ParentId);

				var directReplies = await DbContext.Messages.Where(m => m.ReplyId == messageId).ToListAsync();

				foreach (var reply in directReplies) {
					reply.OriginalBody =
						$"[quote]{record.OriginalBody}\n" +
						$"Message deleted by {ContextUser.ApplicationUser.DisplayName} on {DateTime.Now.ToString("MMMM dd, yyyy")}[/quote]" +
						reply.OriginalBody;

					reply.ReplyId = 0;

					DbContext.Update(reply);
				}

				await DbContext.SaveChangesAsync();
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

			await DbContext.SaveChangesAsync();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> PinMessage(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = await DbContext.Messages.FindAsync(messageId);

			if (record == null) {
				serviceResponse.Error(string.Empty, $@"No record was found with the id '{messageId}'");
				return serviceResponse;
			}

			if (record.ParentId > 0)
				messageId = record.ParentId;

			var existingRecord = await DbContext.Pins.FirstOrDefaultAsync(p => p.MessageId == messageId && p.UserId == ContextUser.ApplicationUser.Id);

			if (existingRecord == null) {
				var pinRecord = new DataModels.Pin {
					MessageId = messageId,
					Time = DateTime.Now,
					UserId = ContextUser.ApplicationUser.Id
				};

				await DbContext.Pins.AddAsync(pinRecord);
			}
			else
				DbContext.Pins.Remove(existingRecord);

			await DbContext.SaveChangesAsync();

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> AddThought(InputModels.ThoughtInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var messageRecord = await DbContext.Messages.FindAsync(input.MessageId);

			if (messageRecord == null)
				serviceResponse.Error(string.Empty, $@"No message was found with the id '{input.MessageId}'");

			var smileyRecord = await DbContext.Smileys.FindAsync(input.SmileyId);

			if (messageRecord == null)
				serviceResponse.Error(string.Empty, $@"No smiley was found with the id '{input.SmileyId}'");

			if (!serviceResponse.Success)
				return serviceResponse;

			var existingRecord = await DbContext.MessageThoughts
				.SingleOrDefaultAsync(mt => 
					mt.MessageId == messageRecord.Id
					&& mt.SmileyId == smileyRecord.Id
					&& mt.UserId == ContextUser.ApplicationUser.Id);

			if (existingRecord == null) {
				var messageThought = new DataModels.MessageThought {
					MessageId = messageRecord.Id,
					SmileyId = smileyRecord.Id,
					UserId = ContextUser.ApplicationUser.Id
				};

				await DbContext.MessageThoughts.AddAsync(messageThought);

				if (messageRecord.PostedById != ContextUser.ApplicationUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = messageRecord.Id,
						UserId = messageRecord.PostedById,
						TargetUserId = ContextUser.ApplicationUser.Id,
						Time = DateTime.Now,
						Type = Enums.ENotificationType.Thought,
						Unread = true,
					};

					await DbContext.Notifications.AddAsync(notification);
				}
			}
			else
				DbContext.MessageThoughts.Remove(existingRecord);

			await DbContext.SaveChangesAsync();

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(input.MessageId);
			return serviceResponse;
		}
		
		public async Task<ServiceModels.ServiceResponse> FinishMigration(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = await DbContext.Messages.FindAsync(messageId);

			if (record == null)
				serviceResponse.Error(string.Empty, $@"No record was found with the id '{messageId}'");

			if (record.ParentId > 0) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = record.ParentId });
				return serviceResponse;
			}

			if (record.Processed) {
				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.Id });
				return serviceResponse;
			}

			serviceResponse.RedirectPath = UrlHelper.DirectMessage(record.Id);

			MigrateMessageRecord(record);

			DbContext.Participants.Add(new DataModels.Participant {
				MessageId = record.Id,
				Time = record.TimePosted,
				UserId = record.PostedById
			});

			var replies = await DbContext.Messages.Where(m => m.LegacyParentId == record.LegacyId).OrderBy(m => m.TimePosted).ToListAsync();

			foreach (var reply in replies) {
				reply.ParentId = record.Id;
				MigrateMessageRecord(reply);

				DbContext.Participants.Add(new DataModels.Participant {
					MessageId = record.Id,
					Time = reply.TimePosted,
					UserId = reply.PostedById
				});
			}

			if (replies.Any()) {
				record.LastReplyId = replies.Last().Id;
				record.LastReplyById = replies.Last().PostedById;
				record.LastReplyPosted = replies.Last().TimePosted;
			}

			DbContext.Update(record);
			await DbContext.SaveChangesAsync();

			return serviceResponse;
		}

		async Task<InputModels.ProcessedMessageInput> ProcessMessageInput(ServiceModels.ServiceResponse serviceResponse, string messageBody) {
			var processedMessage = PreProcessMessageInput(messageBody);
			await PreProcessSmileys(processedMessage);
			ParseBBC(processedMessage);
			await ProcessSmileys(processedMessage);
			ProcessMessageBodyUrls(processedMessage);
			await FindMentionedUsers(processedMessage);
			PostProcessMessageInput(processedMessage);

			return processedMessage;
		}

		/// <summary>
		/// Some minor housekeeping on the message before we get into the heavy lifting.
		/// </summary>
		InputModels.ProcessedMessageInput PreProcessMessageInput(string messageBody) {
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
		void ParseBBC(InputModels.ProcessedMessageInput processedMessageInput) {
			var displayBody = processedMessageInput.DisplayBody;

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

			processedMessageInput.DisplayBody = parser.ToHtml(displayBody);
		}

		/// <summary>
		/// Attempt to replace URLs in the message body with something better
		/// </summary>
		void ProcessMessageBodyUrls(InputModels.ProcessedMessageInput processedMessageInput) {
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

		async Task PreProcessSmileys(InputModels.ProcessedMessageInput processedMessageInput) {
			var smileys = await DbContext.Smileys.Where(s => s.Code != null).ToListAsync();

			for (var i = 0; i < smileys.Count(); i++)
				processedMessageInput.DisplayBody = new Regex("(^| )(" + Regex.Escape(smileys[i].Code) + ")( |$)", RegexOptions.Compiled | RegexOptions.Multiline).Replace(processedMessageInput.DisplayBody, $"$1SMILEY_{i}_INDEX$3");
		}

		async Task ProcessSmileys(InputModels.ProcessedMessageInput processedMessageInput) {
			var smileys = await DbContext.Smileys.Where(s => s.Code != null).ToListAsync();

			for (var i = 0; i < smileys.Count(); i++)
				processedMessageInput.DisplayBody = new Regex($@"(^| )(SMILEY_{i}_INDEX)( |$)", RegexOptions.Compiled | RegexOptions.Multiline).Replace(processedMessageInput.DisplayBody, "$1<img src='" + smileys[i].Path + "' />$3");
		}

		/// <summary>
		/// Attempt to replace the ugly URL with a human readable title.
		/// </summary>
		ServiceModels.RemoteUrlReplacement GetRemoteUrlReplacement(string remoteUrl) {
			var remotePageDetails = GetRemotePageDetails(remoteUrl);
			remotePageDetails.Title = remotePageDetails.Title.Replace("$", "&#36;");

			const string youtubePattern = @"(?:https?:\/\/)?(?:www\.)?(?:(?:(?:youtube.com\/watch\?[^?]*v=|youtu.be\/)([\w\-]+))(?:[^\s?]+)?)";
			const string youtubeIframePartial = "<iframe type='text/html' title='YouTube video player' class='youtubePlayer' src='http://www.youtube.com/embed/{0}' frameborder='0' allowfullscreen='1'></iframe>";
			const string gifvPartial = "<video autoplay loop><source src='{0}.webm' type='video/webm' /><source src='{0}.mp4' type='video/mp4' /></video>";

			var regexYoutube = new Regex(youtubePattern);
			var regexGifv = new Regex("(^| )((https?\\://){1}\\S+)\\.gifv", RegexOptions.Compiled | RegexOptions.Multiline);
			var regexUrl = new Regex("(^| )((https?\\://){1}\\S+)", RegexOptions.Compiled | RegexOptions.Multiline);

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
			// or is it a gifv link
			else if (regexGifv.Match(remoteUrl).Success) {
				var gifvId = regexGifv.Match(remoteUrl).Groups[2].Value;
				var gifvEmbedded = string.Format(gifvPartial, gifvId);

				return new ServiceModels.RemoteUrlReplacement {
					Regex = regexGifv,
					ReplacementText = " <a target='_blank' href='" + remoteUrl + "'>" + remotePageDetails.Title + "</a>",
					Card = $@"<div class=""embedded-video"">{gifvEmbedded}</div>"
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
		ServiceModels.RemotePageDetails GetRemotePageDetails(string remoteUrl) {
			var returnResult = new ServiceModels.RemotePageDetails {
				Title = remoteUrl
			};

			var siteWithoutHash = remoteUrl.Split('#')[0];

			HtmlDocument document = null;

			var client = new HtmlWeb() {
				UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36"
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
				try {
					document = await client.LoadFromWebAsync(siteWithoutHash);
				}
				// HtmlAgilityPack throws a generic exception when it fails to load a page.
				catch (Exception) { }
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
		async Task FindMentionedUsers(InputModels.ProcessedMessageInput processedMessageInput) {
			var regexUsers = new Regex(@"@(\S+)");

			var matches = 0;

			foreach (Match regexMatch in regexUsers.Matches(processedMessageInput.DisplayBody)) {
				matches++;

				// DoS prevention
				if (matches > 10)
					break;

				var matchedTag = regexMatch.Groups[1].Value;

				var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UserName.ToLower() == matchedTag.ToLower());

				// try to guess what they meant
				if (user == null)
					user = await DbContext.Users.FirstOrDefaultAsync(u => u.UserName.ToLower().Contains(matchedTag.ToLower()));

				if (user != null) {
					if (user.Id != ContextUser.ApplicationUser.Id)
						processedMessageInput.MentionedUsers.Add(user.Id);

					// Eventually link to user profiles
					// returnObject.ProcessedBody = Regex.Replace(returnObject.ProcessedBody, @"@" + regexMatch.Groups[1].Value, "<a href='/Profile/Details/" + user.UserId + "' class='user'>" + user.DisplayName + "</span>");
				}
			}
		}

		/// <summary>
		/// Minor post processing
		/// </summary>
		void PostProcessMessageInput(InputModels.ProcessedMessageInput processedMessageInput) {
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

		async Task<DataModels.Message> CreateMessageRecord(InputModels.ProcessedMessageInput processedMessage, DataModels.Message replyRecord) {
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

					parentMessage = await DbContext.Messages.FindAsync(replyRecord.ParentId);

					if (parentMessage == null)
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

				PostedById = ContextUser.ApplicationUser.Id,
				EditedById = ContextUser.ApplicationUser.Id,
				LastReplyById = ContextUser.ApplicationUser.Id,

				ParentId = parentId,
				ReplyId = replyId,
			};

			await DbContext.Messages.AddAsync(record);

			await DbContext.SaveChangesAsync();

			if (replyRecord != null) {
				replyRecord.LastReplyId = record.Id;
				replyRecord.LastReplyById = ContextUser.ApplicationUser.Id;
				replyRecord.LastReplyPosted = currentTime;

				DbContext.Update(replyRecord);

				if (replyRecord.PostedById != ContextUser.ApplicationUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = record.Id,
						UserId = replyRecord.PostedById,
						TargetUserId = ContextUser.ApplicationUser.Id,
						Time = DateTime.Now,
						Type = Enums.ENotificationType.Quote,
						Unread = true,
					};

					await DbContext.Notifications.AddAsync(notification);
				}
			}

			if (parentMessage != null && parentMessage.Id != replyRecord.Id) {
				parentMessage.LastReplyId = record.Id;
				parentMessage.LastReplyById = ContextUser.ApplicationUser.Id;
				parentMessage.LastReplyPosted = currentTime;

				DbContext.Update(parentMessage);

				if (parentMessage.PostedById != ContextUser.ApplicationUser.Id) {
					var notification = new DataModels.Notification {
						MessageId = record.Id,
						UserId = parentMessage.PostedById,
						TargetUserId = ContextUser.ApplicationUser.Id,
						Time = DateTime.Now,
						Type = Enums.ENotificationType.Reply,
						Unread = true,
					};

					await DbContext.Notifications.AddAsync(notification);
				}
			}

			await DbContext.SaveChangesAsync();

			var topicId = parentId == 0 ? record.Id : parentId;

			var participation = await DbContext.Participants.SingleOrDefaultAsync(r => r.MessageId == topicId && r.UserId == ContextUser.ApplicationUser.Id);

			if (participation == null) {
				DbContext.Participants.Add(new DataModels.Participant {
					MessageId = topicId,
					UserId = ContextUser.ApplicationUser.Id,
					Time = DateTime.Now
				});
			}
			else {
				participation.Time = DateTime.Now;
				DbContext.Update(participation);
			}

			await DbContext.SaveChangesAsync();

			return record;
		}

		async Task UpdateMessageRecord(InputModels.ProcessedMessageInput message, DataModels.Message record) {
			record.OriginalBody = message.OriginalBody;
			record.DisplayBody = message.DisplayBody;
			record.ShortPreview = message.ShortPreview;
			record.LongPreview = message.LongPreview;
			record.Cards = message.Cards;
			record.TimeEdited = DateTime.Now;
			record.EditedById = ContextUser.ApplicationUser.Id;
			record.Processed = true;

			DbContext.Update(record);

			await DbContext.SaveChangesAsync();
		}

		void MigrateMessageRecord(DataModels.Message record) {
			var processMessageTask = ProcessMessageInput(new ServiceModels.ServiceResponse(), record.OriginalBody);
			var replyTask = DbContext.Messages.SingleOrDefaultAsync(m => m.LegacyId == record.LegacyReplyId);
			var postedByTask = DbContext.Users.SingleOrDefaultAsync(u => u.LegacyId == record.LegacyPostedById);
			var editedByTask = DbContext.Users.SingleOrDefaultAsync(u => u.LegacyId == record.LegacyPostedById);

			Task.WaitAll(processMessageTask, replyTask, postedByTask, editedByTask);

			var message = processMessageTask.Result;

			record.OriginalBody = message.OriginalBody;
			record.DisplayBody = message.DisplayBody;
			record.ShortPreview = message.ShortPreview;
			record.LongPreview = message.LongPreview;
			record.Cards = message.Cards;
			record.TimeEdited = DateTime.Now;
			record.EditedById = ContextUser.ApplicationUser.Id;
			record.Processed = true;

			record.ReplyId = replyTask.Result?.Id ?? 0;
			record.PostedById = postedByTask.Result?.Id ?? string.Empty;
			record.EditedById = postedByTask.Result?.Id ?? string.Empty;

			DbContext.Update(record);
		}
	}
}