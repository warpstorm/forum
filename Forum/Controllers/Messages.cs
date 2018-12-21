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
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Messages : Controller {
		ApplicationDbContext DbContext { get; }
		BoardRepository BoardRepository { get; }
		MessageRepository MessageRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SmileyRepository SmileyRepository { get; }
		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Messages(
			ApplicationDbContext dbContext,
			BoardRepository boardRepository,
			MessageRepository messageRepository,
			SettingsRepository settingsRepository,
			SmileyRepository smileyRepository,
			IActionContextAccessor actionContextAccessor,
			IForumViewResult forumViewResult,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			BoardRepository = boardRepository;
			MessageRepository = messageRepository;
			SettingsRepository = settingsRepository;
			SmileyRepository = smileyRepository;
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			ViewData["Smileys"] = SmileyRepository.GetSelectorList();

			var board = BoardRepository.First(item => item.Id == id);

			if (Request.Query.TryGetValue("source", out var source)) {
				return await Create(new InputModels.MessageInput { BoardId = id, Body = source });
			}

			var viewModel = new ViewModels.Messages.CreateTopicPage {
				BoardId = id
			};

			return ForumViewResult.ViewResult(this, viewModel);
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
					foreach (var board in BoardRepository) {
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
				var viewModel = new ViewModels.Messages.CreateTopicPage() {
					BoardId = input.BoardId,
					Body = input.Body
				};

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, viewModel); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			ViewData["Smileys"] = SmileyRepository.GetSelectorList();

			var record = await DbContext.Messages.SingleOrDefaultAsync(m => m.Id == id);

			if (record is null) {
				throw new HttpNotFoundError();
			}

			var viewModel = new ViewModels.Messages.EditMessagePage {
				Id = id,
				Body = record.OriginalBody
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.EditMessage(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = new ViewModels.Messages.CreateTopicPage {
					Body = input.Body
				};

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, viewModel); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.DeleteMessage(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> AddThought(InputModels.ThoughtInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.AddThought(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult Admin(InputModels.Continue input = null) => ForumViewResult.ViewResult(this);

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IActionResult> ProcessMessages(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.ProcessMessages();

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

			return ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IActionResult> ReprocessMessages(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.ReprocessMessages();

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

			return ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult RecountReplies(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.RecountReplies();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.RecountReplies),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else {
				MessageRepository.RecountRepliesContinue(input);
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

			return ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult RebuildParticipants(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.RebuildParticipants();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.RebuildParticipants),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else {
				MessageRepository.RebuildParticipantsContinue(input);
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

			return ForumViewResult.ViewResult(this, "Delay", viewModel);
		}
	}
}
