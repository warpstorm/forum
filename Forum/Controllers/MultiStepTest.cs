using Forum.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Forum.Controllers {
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
			var viewModel = new List<string> {
				UrlHelper.Action(nameof(TestStep1)),
				UrlHelper.Action(nameof(TestStep2)),
				UrlHelper.Action(nameof(TestStep3)),
			};

			return await ForumViewResult.ViewResult(this, "MultiStep", viewModel);
		}

		[HttpPost]
		public IActionResult TestStep1(Models.ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				return Ok(new Models.ControllerModels.Administration.Step {
					ActionName = "Step 1",
					ActionNote = "Running step 1 of multi-step test",
					Take = 5,
					TotalPages = 5,
					TotalRecords = 23,
				});
			}

			// Do something with the metrics here, i.e. skip = take * page

			Thread.Sleep(1000);
			return Ok();
		}

		[HttpPost]
		public IActionResult TestStep2(Models.ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				return Ok(new Models.ControllerModels.Administration.Step {
					ActionName = "Step 2",
					ActionNote = "Running step 2 of multi-step test",
					Take = 3,
					TotalPages = 4,
					TotalRecords = 12,
				});
			}

			Thread.Sleep(2000);
			return Ok();
		}

		[HttpPost]
		public IActionResult TestStep3(Models.ControllerModels.Administration.Page input) {
			if (input.CurrentPage < 0) {
				return Ok(new Models.ControllerModels.Administration.Step {
					ActionName = "Step 3",
					ActionNote = "Running step 3 of multi-step test",
					Take = 3,
					TotalPages = 6,
					TotalRecords = 17,
				});
			}

			Thread.Sleep(3000);
			return Ok();
		}
	}
}
