using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Services;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;

	public class Quotes : Controller {
		QuoteRepository QuoteRepository { get; }
		ForumViewResult ForumViewResult { get; }

		public Quotes(
			QuoteRepository quoteRepository,
			ForumViewResult forumViewResult
		) {
			QuoteRepository = quoteRepository;
			ForumViewResult = forumViewResult;
		}

		[ActionLog]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = await QuoteRepository.Index();
			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await QuoteRepository.Create(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> Edit(ControllerModels.Quotes.QuotesInput input) {
			if (ModelState.IsValid) {
				var result = await QuoteRepository.Edit(input);
				ModelState.AddModelErrors(result.Errors);
			}

			if (ModelState.IsValid) {
				TempData[Constants.InternalKeys.StatusMessage] = "Changes saved.";
			}
			else {
				TempData[Constants.InternalKeys.StatusMessage] = "Errors were encountered while updating quotes.";
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await QuoteRepository.Delete(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}
	}
}
