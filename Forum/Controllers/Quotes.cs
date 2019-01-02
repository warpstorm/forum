using Forum.Interfaces.Services;
using Forum.Repositories;
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

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public IActionResult Index() {
			var viewModel = QuoteRepository.Index();
			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult Create(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = QuoteRepository.Create(id);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return ForumViewResult.RedirectToReferrer(this);
			}
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> Edit(InputModels.QuotesInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await QuoteRepository.Edit(input);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return ForumViewResult.RedirectToReferrer(this);
			}
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public IActionResult Delete(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = QuoteRepository.Delete(id);
				return ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				return ForumViewResult.RedirectToReferrer(this);
			}
		}
	}
}
