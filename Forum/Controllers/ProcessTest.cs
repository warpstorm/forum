using Forum.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Forum.Controllers {
	public class ProcessTest : Controller {
		ForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public ProcessTest(
			ForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public async Task<IActionResult> Test() {
			var viewModel = new List<string> {
				UrlHelper.Action(nameof(TestStage1)),
				UrlHelper.Action(nameof(TestStage2)),
				UrlHelper.Action(nameof(TestStage3)),
			};

			return await ForumViewResult.ViewResult(this, "Process", viewModel);
		}

		[HttpPost]
		public IActionResult TestStage1(Models.ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				return Ok(new Models.ControllerModels.Administration.ProcessStage {
					ActionName = "Stage 1",
					ActionNote = "Running stage 1 of process test",
					Take = 5,
					TotalSteps = 5,
					TotalRecords = 23,
				});
			}

			// Do something with the metrics here, i.e. skip = take * page

			Thread.Sleep(1000);
			return Ok();
		}

		[HttpPost]
		public IActionResult TestStage2(Models.ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				return Ok(new Models.ControllerModels.Administration.ProcessStage {
					ActionName = "Stage 2",
					ActionNote = "Running stage 2 of process test",
					Take = 3,
					TotalSteps = 4,
					TotalRecords = 12,
				});
			}

			Thread.Sleep(2000);
			return Ok();
		}

		[HttpPost]
		public IActionResult TestStage3(Models.ControllerModels.Administration.ProcessStep input) {
			if (input.CurrentStep < 0) {
				return Ok(new Models.ControllerModels.Administration.ProcessStage {
					ActionName = "Stage 3",
					ActionNote = "Running stage 3 of process test",
					Take = 3,
					TotalSteps = 6,
					TotalRecords = 17,
				});
			}

			Thread.Sleep(3000);
			return Ok();
		}
	}
}
