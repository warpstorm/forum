using Forum.Services;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels;

	public class Events : Controller {
		public ApplicationDbContext DbContext { get; set; }
		public ForumViewResult ForumViewResult { get; set; }

		public Events(
			ApplicationDbContext dbContext,
			ForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			ForumViewResult = forumViewResult;
		}

		[HttpGet]
		public async Task<IActionResult> Create() {
			var viewModel = new ViewModels.Events.CreateForm();
			return await ForumViewResult.ViewResult(this, viewModel);
		}
	}
}
