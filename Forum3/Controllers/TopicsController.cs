using System;
using System.Threading.Tasks;
using Forum3.Data;
using Forum3.Helpers;
using Forum3.Services;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc;

namespace Forum3.Controllers {
	[Authorize]
	public class TopicsController : Controller
    {
		public ApplicationDbContext _dbContext { get; set; }
		public TopicService _topicService { get; set; }

		public TopicsController(ApplicationDbContext dbContext, TopicService topicService) {
			_dbContext = dbContext;
			_topicService = topicService;
		}

		// GET: Topics
		[AllowAnonymous]
		public async Task<IActionResult> Index() {
			var skip = 0;

			if (HttpContext.Request.Query.ContainsKey("skip"))
				skip = Convert.ToInt32(HttpContext.Request.Query["skip"]);

			var take = 15;

			if (HttpContext.Request.Query.ContainsKey("take"))
				take = Convert.ToInt32(HttpContext.Request.Query["take"]);

			var viewModel = await _topicService.ConstructTopicIndexAsync(skip, take);

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

				var topic = await _topicService.ConstructTopicAsync(id, page, skip, take, jumpToLatest);

				return View(topic);
			}
			catch (ChildMessageException e) {
				return Redirect(Url.RouteUrl(new {
					action = "Topic",
					id = e.ParentId
				}) + "#message" + e.ChildId);
			}
			catch (Exception e) {
				ViewBag.Exception = e.Message;
				return View();
			}
		}

		// GET: Topics/Create
		public IActionResult Create() {
			return View();
		}
	}
}
