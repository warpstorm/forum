using System;
using System.Threading.Tasks;
using Forum3.Helpers;
using Forum3.Services;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Forum3.Controllers {
	[Authorize]
	public class TopicsController : Controller {
		public TopicRepository _topics { get; set; }

		public TopicsController(TopicRepository topicRepo) {
			_topics = topicRepo;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Index() {
			var skip = 0;

			if (HttpContext.Request.Query.ContainsKey("skip"))
				skip = Convert.ToInt32(HttpContext.Request.Query["skip"]);

			var take = 15;

			if (HttpContext.Request.Query.ContainsKey("take"))
				take = Convert.ToInt32(HttpContext.Request.Query["take"]);

			var viewModel = await _topics.GetTopicIndexAsync(skip, take);

			return View(viewModel);
		}

		public async Task<IActionResult> Display(int id, int page = 1) {
			try {
				var jumpToLatest = false;

				if (page == 0) {
					page = 1;
					jumpToLatest = true;
				}

				int take = 15;
				int skip = (page * take) - take;

				var viewModel = await _topics.GetTopicAsync(id, page, skip, take, jumpToLatest);

				return View(viewModel);
			}
			catch (ChildMessageException e) {
				return Redirect(Url.RouteUrl(new {
					action = "Topic",
					id = e.ParentId
				}) + "#message" + e.ChildId);
			}
		}

		public IActionResult Create() {
			return View();
		}
	}
}