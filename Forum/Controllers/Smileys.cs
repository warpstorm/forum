using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;
	using ViewModels = Models.ViewModels.Smileys;

	[Authorize(Roles = Constants.InternalKeys.Admin)]
	public class Smileys : Controller {
		ApplicationDbContext DbContext { get; }
		SmileyRepository SmileyRepository { get; }
		ForumViewResult ForumViewResult { get; }

		public Smileys(
			ApplicationDbContext dbContext,
			SmileyRepository smileyRepository,
			ForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			SmileyRepository = smileyRepository;
			ForumViewResult = forumViewResult;
		}

		[ActionLog]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new ViewModels.IndexPage();

			foreach (var smiley in await SmileyRepository.Records()) {
				var sortColumn = smiley.SortOrder / 1000;
				var sortRow = smiley.SortOrder % 1000;

				viewModel.Smileys.Add(new ViewModels.IndexItem {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Thought = smiley.Thought,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[ActionLog]
		[HttpGet]
		public async Task<IActionResult> Create() {
			var viewModel = new ViewModels.CreatePage();
			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(ControllerModels.Smileys.CreateSmileyInput input) {
			if (ModelState.IsValid) {
				var result = await SmileyRepository.Create(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					TempData[Constants.InternalKeys.StatusMessage] = $"Smiley '{input.File.FileName}' was added with code '{input.Code}'.";

					var referrer = ForumViewResult.GetReferrer(this);
					return Redirect(referrer);
				}
			}

			var viewModel = new ViewModels.CreatePage {
				Code = input.Code,
				Thought = input.Thought
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(ControllerModels.Smileys.EditSmileysInput input) {
			if (ModelState.IsValid) {
				var result = await SmileyRepository.Edit(input);
				ModelState.AddModelErrors(result.Errors);

				if (ModelState.IsValid) {
					TempData[Constants.InternalKeys.StatusMessage] = $"Smileys were updated.";

					var referrer = ForumViewResult.GetReferrer(this);
					return Redirect(referrer);
				}
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await SmileyRepository.Delete(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}
	}
}