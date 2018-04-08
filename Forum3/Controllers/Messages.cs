using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Models.InputModels;
using Forum3.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using ViewModels = Models.ViewModels.Messages;

	public class Messages : ForumController {
		ApplicationDbContext DbContext { get; }
		MessageRepository MessageRepository { get; }
		SettingsRepository SettingsRepository { get; }
		IUrlHelper UrlHelper { get; }

		public Messages(
			ApplicationDbContext dbContext,
			MessageRepository messageRepository,
			SettingsRepository settingsRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			MessageRepository = messageRepository;
			SettingsRepository = settingsRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			var board = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == id);

			if (board is null)
				throw new Exception($"A record does not exist with ID '{id}'");

			var viewModel = new ViewModels.CreateTopicPage {
				BoardId = id,
				CancelPath = Referrer
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public IActionResult Create(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = MessageRepository.CreateTopic(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = new ViewModels.CreateTopicPage() {
				BoardId = input.BoardId,
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var record = await DbContext.Messages.SingleOrDefaultAsync(m => m.Id == id);

			if (record is null)
				throw new Exception($"A record does not exist with ID '{id}'");

			var viewModel = new ViewModels.EditMessagePage {
				Id = id,
				Body = record.OriginalBody,
				CancelPath = Referrer
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public IActionResult Edit(MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = MessageRepository.EditMessage(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = new ViewModels.CreateTopicPage() {
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			var serviceResponse = await MessageRepository.DeleteMessage(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> AddThought(ThoughtInput input) {
			var serviceResponse = await MessageRepository.AddThought(input);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public IActionResult Migrate(int id, int page) {
			var viewModel = GetMigratePageModel(id, page);
			return View("Delay", viewModel);
		}

		public Models.ViewModels.Delay GetMigratePageModel(int id, int page) {
			var record = DbContext.Messages.Find(id);

			if (record is null)
				throw new Exception($@"No record was found with the id '{id}'");

			var viewModel = new Models.ViewModels.Delay {
				ActionName = "Migrating your Topic",
				ActionNote = "Filing your request with the Imperial Archives.",
				CurrentPage = page,
				NextAction = UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = record.Id, page = page + 1 })
			};

			if (record.LegacyParentId > 0) {
				var parentRecord = DbContext.Messages.Where(r => r.LegacyId == record.LegacyParentId).FirstOrDefault();

				if (parentRecord is null)
					throw new ArgumentException($"No parent record found for legacy id '{record.LegacyParentId}'");

				viewModel.NextAction = UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = parentRecord.Id });
				return viewModel;
			}

			if (record.ParentId > 0) {
				var parentRecord = DbContext.Messages.Where(r => r.Id == record.ParentId).FirstOrDefault();
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.Migrate), nameof(Messages), new { id = parentRecord.Id });
				return viewModel;
			}

			var messageQuery = from message in DbContext.Messages
							   where message.Id == record.Id || message.ParentId == record.Id || (record.LegacyId != 0 && message.LegacyParentId == record.LegacyId)
							   select message.Id;

			var messageIds = messageQuery.ToList();
			var messageCount = messageIds.Count();

			var totalPages = Convert.ToInt32(Math.Ceiling(1.0D * messageCount / SettingsRepository.MessagesPerPage()));
			viewModel.TotalPages = totalPages;

			if (page < 1)
				page = 1;

			if (page > totalPages)
				page = totalPages;

			var take = SettingsRepository.MessagesPerPage();
			var skip = take * (page - 1);

			foreach (var messageId in messageIds.Skip(skip).Take(take)) {
				var messageRecord = DbContext.Messages.Find(messageId);
				MessageRepository.MigrateMessageRecord(messageRecord);
			}

			DbContext.SaveChanges();

			if (page == totalPages)
				viewModel.NextAction = UrlHelper.Action(nameof(Topics.Display), nameof(Topics), new { id = record.Id });

			return viewModel;
		}
	}
}
