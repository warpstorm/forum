using Forum.Annotations;
using Forum.Contexts;
using Forum.Errors;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Messages : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		BoardRepository BoardRepository { get; }
		MessageRepository MessageRepository { get; }
		SmileyRepository SmileyRepository { get; }
		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Messages(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			BoardRepository boardRepository,
			MessageRepository messageRepository,
			SmileyRepository smileyRepository,
			IActionContextAccessor actionContextAccessor,
			IForumViewResult forumViewResult,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			BoardRepository = boardRepository;
			MessageRepository = messageRepository;
			SmileyRepository = smileyRepository;
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[ActionLog("is starting a new topic.")]
		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var board = (await BoardRepository.Records()).First(item => item.Id == id);

			if (Request.Query.TryGetValue("source", out var source)) {
				return await Create(new InputModels.MessageInput { BoardId = id, Body = source });
			}

			var viewModel = new ViewModels.Messages.CreateTopicForm {
				Id = "0",
				BoardId = id.ToString()
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				if (Request.Method == "GET" && input.BoardId != null) {
					input.SelectedBoards.Add((int)input.BoardId);
				}
				else {
					foreach (var board in await BoardRepository.Records()) {
						if (Request.Form.TryGetValue("Selected_" + board.Id, out var boardSelected)) {
							if (boardSelected == "True") {
								input.SelectedBoards.Add(board.Id);
							}
						}
					}
				}

				var serviceResponse = await MessageRepository.CreateTopic(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

				var viewModel = new ViewModels.Messages.CreateTopicForm {
					Id = "0",
					BoardId = input.BoardId.ToString(),
					Body = input.Body
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Reply(int id = 0) {
			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var record = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.ReplyForm {
				Id = id.ToString(),
				ElementId = $"message-reply-{id}"
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> ReplyPartial(int id) {
			var record = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.ReplyForm {
				Id = id.ToString(),
				ElementId = $"message-reply-{id}"
			};

			return await ForumViewResult.ViewResult(this, "_MessageForm", viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Reply(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.CreateReply(input);

				if (input.SideLoad) {
					foreach (var kvp in serviceResponse.Errors) {
						ModelState.AddModelError(kvp.Key, kvp.Value);
					}
				}
				else {
					return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
				}
			}

			if (input.SideLoad) {
				var errors = ModelState.Keys.Where(k => ModelState[k].Errors.Count > 0).Select(k => new { propertyName = k, errorMessage = ModelState[k].Errors[0].ErrorMessage });
				return new JsonResult(errors);
			}
			else {
				return await FailureCallback();
			}

			async Task<IActionResult> FailureCallback() {
				ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

				var viewModel = new ViewModels.Messages.ReplyForm {
					Id = input.Id.ToString(),
					Body = input.Body,
					ElementId = $"message-reply-{input.Id}"
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}


		[ActionLog("is editing a message.")]
		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			ViewData["Smileys"] = await SmileyRepository.GetSelectorList();

			var record = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.EditMessageForm {
				Id = id.ToString(),
				Body = record.OriginalBody,
				ElementId = $"edit-message-{id}"
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> EditPartial(int id) {
			var record = await DbContext.Messages.FirstOrDefaultAsync(m => m.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.EditMessageForm {
				Id = id.ToString(),
				Body = record.OriginalBody,
				ElementId = $"edit-message-{id}"
			};

			return await ForumViewResult.ViewResult(this, "_MessageForm", viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.EditMessage(input);

				if (input.SideLoad) {
					foreach (var kvp in serviceResponse.Errors) {
						ModelState.AddModelError(kvp.Key, kvp.Value);
					}
				}
				else {
					return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
				}
			}

			if (input.SideLoad) {
				var errors = ModelState.Keys.Where(k => ModelState[k].Errors.Count > 0).Select(k => new { propertyName = k, errorMessage = ModelState[k].Errors[0].ErrorMessage });
				return new JsonResult(errors);
			}
			else {
				return await FailureCallback();
			}

			async Task<IActionResult> FailureCallback() {
				var viewModel = new ViewModels.Messages.CreateTopicForm {
					Id = "0",
					Body = input.Body,
					ElementId = "create-topic"
				};

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.DeleteMessage(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> AddThought(int id, int smiley) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.AddThought(id, smiley);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[ActionLog("is viewing a user's message history.")]
		[HttpGet]
		public async Task<IActionResult> History(string id, int pageId = 1) {
			if (string.IsNullOrEmpty(id)) {
				id = UserContext.ApplicationUser.Id;
			}

			var userRecord = (await AccountRepository.Records()).FirstOrDefault(item => item.Id == id);

			if (userRecord is null) {
				throw new HttpNotFoundError();
			}

			var messages = await MessageRepository.GetUserMessages(id, pageId);
			var morePages = true;

			if (messages.Count < UserContext.ApplicationUser.MessagesPerPage) {
				morePages = false;
			}

			messages = messages.OrderByDescending(r => r.TimePosted).ToList();

			var viewModel = new ViewModels.Messages.HistoryPage {
				Id = userRecord.Id,
				DisplayName = userRecord.DecoratedName,
				Email = userRecord.Email,
				CurrentPage = pageId,
				MorePages = morePages,
				ShowFavicons = UserContext.ApplicationUser.ShowFavicons,
				Messages = messages,
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Admin(InputModels.Continue input = null) => await ForumViewResult.ViewResult(this);

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> ProcessMessages(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = await MessageRepository.ProcessMessages();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.ProcessMessages),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else {
				await MessageRepository.ProcessMessagesContinue(input);
			}

			var viewModel = new ViewModels.Delay {
				ActionName = "Processing Messages",
				ActionNote = "Processing message text, loading external sites, replacing smiley codes.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.ProcessMessages), nameof(Messages), input);
			}

			return await ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> ReprocessMessages(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = await MessageRepository.ReprocessMessages();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.ReprocessMessages),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else {
				await MessageRepository.ReprocessMessagesContinue(input);
			}

			var viewModel = new ViewModels.Delay {
				ActionName = "Reprocessing Messages",
				ActionNote = "Processing message text, loading external sites, replacing smiley codes.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.ReprocessMessages), nameof(Messages), input);
			}

			return await ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> RecountReplies(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = await MessageRepository.RecountReplies();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.RecountReplies),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else {
				await MessageRepository.RecountRepliesContinue(input);
			}

			var viewModel = new ViewModels.Delay {
				ActionName = "Recounting Replies",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.RecountReplies), nameof(Messages), input);
			}

			return await ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> RebuildParticipants(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = await MessageRepository.RebuildParticipants();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.RebuildParticipants),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else {
				await MessageRepository.RebuildParticipantsContinue(input);
			}

			var viewModel = new ViewModels.Delay {
				ActionName = "Rebuilding participants",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.RebuildParticipants), nameof(Messages), input);
			}

			return await ForumViewResult.ViewResult(this, "Delay", viewModel);
		}
	}
}
