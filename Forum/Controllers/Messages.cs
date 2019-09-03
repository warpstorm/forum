using Forum.Controllers.Annotations;
using Forum.Core.Models.Errors;
using Forum.Data.Contexts;
using Forum.Extensions;
using Forum.Services;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;
	using HubModels = Models.HubModels;
	using ViewModels = Models.ViewModels;

	public class Messages : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		MessageRepository MessageRepository { get; }
		TopicRepository TopicRepository { get; }
		IHubContext<ForumHub> ForumHub { get; }

		public Messages(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			BoardRepository boardRepository,
			MessageRepository messageRepository,
			TopicRepository topicRepository,
			IHubContext<ForumHub> forumHub
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			BoardRepository = boardRepository;
			MessageRepository = messageRepository;
			TopicRepository = topicRepository;
			ForumHub = forumHub;
		}

		/// <summary>
		/// Retrieves a specific message. Useful for API calls.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Display(int id) {
			var message = DbContext.Messages.Find(id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			var topicId = message.TopicId;
			await BoardRepository.GetTopicBoards(topicId);

			var messageIds = new List<int> { id };
			var messages = await MessageRepository.GetMessages(messageIds);

			var viewModel = new ViewModels.Topics.TopicDisplayPartialPage {
				Latest = DateTime.Now.Ticks,
				Messages = messages
			};

			return View("../Topics/DisplayPartial", viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Reply(int id = 0) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.ReplyForm {
				Id = id.ToString(),
				ElementId = $"message-reply-{id}"
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Reply(ControllerModels.Messages.CreateReplyInput input) {
			if (input.Id > 0) {
				var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

				if (message is null || message.Deleted) {
					throw new HttpNotFoundError();
				}
			}

			if (ModelState.IsValid) {
				var result = await MessageRepository.CreateReply(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					var hubAction = "new-reply";

					if (result.Merged) {
						hubAction = "updated-message";
					}

					await ForumHub.Clients.All.SendAsync(hubAction, new HubModels.Message {
						TopicId = result.TopicId,
						MessageId = result.MessageId
					});

					var redirectPath = Url.DisplayMessage(result.TopicId, result.MessageId);
					return Redirect(redirectPath);
				}
			}

			var viewModel = new ViewModels.Messages.ReplyForm {
				Id = input.Id.ToString(),
				Body = input.Body,
				ElementId = $"message-reply-{input.Id}"
			};

			return View(viewModel);
		}

		[SideLoad]
		[HttpGet]
		public async Task<IActionResult> XhrReply(int id) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.ReplyForm {
				Id = id.ToString(),
				TopicId = message.TopicId.ToString(),
				ElementId = $"message-reply-{id}",
				FormAction = nameof(XhrReply)
			};

			return View("_MessageForm", viewModel);
		}

		[SideLoad]
		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> XhrReply(ControllerModels.Messages.CreateReplyInput input) {
			if (input.Id > 0) {
				var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

				if (message is null || message.Deleted) {
					throw new HttpNotFoundError();
				}
			}

			if (ModelState.IsValid) {
				var result = await MessageRepository.CreateReply(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					await ForumHub.Clients.All.SendAsync("new-reply", new HubModels.Message {
						TopicId = result.TopicId,
						MessageId = result.MessageId
					});

					return Ok();
				}
			}

			var errors = ModelState.Keys.Where(k => ModelState[k].Errors.Count > 0).Select(k => new { propertyName = k, errorMessage = ModelState[k].Errors[0].ErrorMessage });
			return new JsonResult(errors);
		}

		[ActionLog("is editing a message.")]
		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			if (message.PostedById != UserContext.ApplicationUser.Id && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			var viewModel = new ViewModels.Messages.EditMessageForm {
				Id = id.ToString(),
				Body = message.OriginalBody,
				ElementId = $"edit-message-{id}"
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(ControllerModels.Messages.EditInput input) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			if (message.PostedById != UserContext.ApplicationUser.Id && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			if (ModelState.IsValid) {
				var result = await MessageRepository.EditMessage(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					await ForumHub.Clients.All.SendAsync("updated-message", new HubModels.Message {
						TopicId = result.TopicId,
						MessageId = result.MessageId
					});

					var redirectPath = Url.DisplayMessage(result.TopicId, result.MessageId);
					return Redirect(redirectPath);
				}
			}

			var viewModel = new ViewModels.Messages.EditMessageForm {
				Id = input.Id.ToString(),
				Body = input.Body,
				ElementId = $"edit-message-{input.Id}"
			};

			return View(viewModel);
		}

		[SideLoad]
		[HttpGet]
		public async Task<IActionResult> XhrEdit(int id) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			if (message.PostedById != UserContext.ApplicationUser.Id && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			var viewModel = new ViewModels.Messages.EditMessageForm {
				Id = id.ToString(),
				Body = message.OriginalBody,
				ElementId = $"edit-message-{id}",
				FormAction = nameof(XhrEdit)
			};

			return View("_MessageForm", viewModel);
		}

		[SideLoad]
		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> XhrEdit(ControllerModels.Messages.EditInput input) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == input.Id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			if (message.PostedById != UserContext.ApplicationUser.Id && !UserContext.IsAdmin) {
				throw new HttpForbiddenError();
			}

			if (ModelState.IsValid) {
				var result = await MessageRepository.EditMessage(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					await ForumHub.Clients.All.SendAsync("updated-message", new HubModels.Message {
						TopicId = result.TopicId,
						MessageId = result.MessageId
					});

					return Ok();
				}
			}

			var errors = ModelState.Keys.Where(k => ModelState[k].Errors.Count > 0).Select(k => new { propertyName = k, errorMessage = ModelState[k].Errors[0].ErrorMessage });
			return new JsonResult(errors);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			var redirectPath = this.GetReferrer();

			if (ModelState.IsValid) {
				var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

				if (message is null || message.Deleted) {
					throw new HttpNotFoundError();
				}

				if (message.PostedById != UserContext.ApplicationUser.Id && !UserContext.IsAdmin) {
					throw new HttpForbiddenError();
				}

				var topic = await DbContext.Topics.SingleAsync(m => m.Id == message.TopicId);

				if (topic.FirstMessageId == message.Id) {
					redirectPath = Url.Action(nameof(Topics.Delete), nameof(Topics), new { topic.Id });
				}
				else {
					await MessageRepository.DeleteMessageFromTopic(message);
					await TopicRepository.RebuildTopicReplies(topic);
					await DbContext.SaveChangesAsync();

					redirectPath = Url.Action(nameof(Topics.Latest), nameof(Topics), new { id = topic.Id });

					await ForumHub.Clients.All.SendAsync("deleted-message", new HubModels.Message {
						TopicId = topic.Id,
						MessageId = message.Id
					});
				}
			}

			return Redirect(redirectPath);
		}

		[HttpGet]
		public async Task<IActionResult> AddThought(int id, int smiley) {
			var message = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (message is null || message.Deleted) {
				throw new HttpNotFoundError();
			}

			if (ModelState.IsValid) {
				var result = await MessageRepository.AddThought(id, smiley);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					await ForumHub.Clients.All.SendAsync("updated-message", new HubModels.Message {
						TopicId = result.TopicId,
						MessageId = result.MessageId
					});

					var redirectPath = Url.DisplayMessage(result.TopicId, result.MessageId);
					return Redirect(redirectPath);
				}
			}

			return this.RedirectToReferrer();
		}

		[ActionLog("is viewing a user's message history.")]
		[HttpGet]
		public async Task<IActionResult> History(string id = "", int page = 1) {
			if (string.IsNullOrEmpty(id)) {
				id = UserContext.ApplicationUser.Id;
			}

			var userRecord = (await AccountRepository.Records()).FirstOrDefault(item => item.Id == id);

			if (userRecord is null) {
				throw new HttpNotFoundError();
			}

			var messages = await MessageRepository.GetUserMessages(id, page);
			var morePages = true;

			if (messages.Count < UserContext.ApplicationUser.MessagesPerPage) {
				morePages = false;
			}

			messages = messages.OrderByDescending(r => r.TimePosted).ToList();

			var viewModel = new ViewModels.Messages.HistoryPage {
				Id = userRecord.Id,
				DisplayName = userRecord.DecoratedName,
				Email = userRecord.Email,
				CurrentPage = page,
				MorePages = morePages,
				ShowFavicons = UserContext.ApplicationUser.ShowFavicons ?? true,
				Messages = messages,
			};

			return View(viewModel);
		}
	}
}
