using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Services;
using Forum3.ViewModels.Boards.Pages;

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
		public async Task<IActionResult> Index() {
			var viewModel = await BoardService.GetBoardIndex();

			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Create() {
			var viewModel = new CreatePage();

			return View(viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> Create(CreatePage input) {
			if (ModelState.IsValid)
				await BoardService.Create(input, ModelState);

			if (ModelState.IsValid)
				return RedirectToAction(nameof(Index));

			return View(input);
		}

		public async Task<IActionResult> Manage() {
			var viewModel = await BoardService.GetBoardTree();

			return View(viewModel);
		}
	}
}