using Forum3.Interfaces.Services;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
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

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult Index() {
			var viewModel = QuoteRepository.Index();
			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = QuoteRepository.Create(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Edit(InputModels.QuotesInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await QuoteRepository.Edit(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = QuoteRepository.Delete(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}
	}
}
