using Forum.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class MultiStepTest : Controller {
		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public MultiStepTest(
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<IActionResult> Test() {
			var viewModel = new ViewModels.MultiStep {
				ActionName = "Testing Multi-step",
				ActionNote = "Running a test on Multi-stepping",
				Action = UrlHelper.Action(nameof(TestWait)),
				Page = 0,
				TotalPages = 12,
				TotalRecords = 30,
				Take = 5,
			};

			return await ForumViewResult.ViewResult(this, "MultiStep", viewModel);
		}

		[HttpPost]
		public IActionResult TestWait(InputModels.MultiStepInput input) {
			try {
				if (ModelState.IsValid) {
					Thread.Sleep(1500);

					if (input.Page == 2) {
						throw new Exception("Hello world");
					}

					return Ok();
				}
			}
			catch (Exception e) {
				return BadRequest(e);
			}

			return BadRequest();
		}
	}
}
