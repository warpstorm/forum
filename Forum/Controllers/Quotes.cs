using Forum.Controllers.Annotations;
using Forum.Services;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;

	public class Quotes : Controller {
		QuoteRepository QuoteRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Quotes(
			QuoteRepository quoteRepository,
			IForumViewResult forumViewResult
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
		public async Task<IActionResult> Edit(InputModels.QuotesInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await QuoteRepository.Edit(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
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
