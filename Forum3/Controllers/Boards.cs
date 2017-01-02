using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Services;
using Forum3.Models.InputModels;

namespace Forum3.Controllers {
	[RequireRemoteHttps]
//	[Authorize(Roles = "Admin")]
	public class Boards : Controller {
		public BoardService BoardService { get; }

		public Boards(BoardService boardService) {
			BoardService = boardService;
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Index() {
			var viewModel = BoardService.GetIndexPage();
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Manage() {
			var viewModel = BoardService.LoadBoardSummaries();
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Create() {
			var viewModel = BoardService.GetCreatePage();
			return View(viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> Create(BoardInput input) {
			if (ModelState.IsValid)
				await BoardService.Create(input, ModelState);

			if (ModelState.IsValid)
				return RedirectToAction(nameof(Index));

			var viewModel = BoardService.GetCreatePage(input);
			return View(viewModel);
		}
	}
}