using Forum.Services;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum.Controllers {
	public class Events : Controller {
		public ApplicationDbContext DbContext { get; set; }
		public ForumViewResult ForumViewResult { get; set; }

		public Events(
			ApplicationDbContext dbContext,
			ForumViewResult forumViewResult
		) {
			DbContext = dbContext;
		}

		[HttpGet]
		public async Task<IActionResult> Create() {
			return await ForumViewResult.ViewResult(this);
		}
	}
}
