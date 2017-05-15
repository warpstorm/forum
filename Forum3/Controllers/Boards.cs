using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;
using Forum3.Models.InputModels;

namespace Forum3.Controllers {
	[Authorize]
	public class Boards : ForumController {
		public BoardService BoardService { get; }

		public Boards(
			BoardService boardService, 
			UserService userService
		) : base(userService) {
			BoardService = boardService;
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Index() {
			var viewModel = await BoardService.IndexPage();
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Manage() {
			var viewModel = await BoardService.ManagePage();
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Create() {
			var viewModel = await BoardService.CreatePage();
			return View(viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> Create(BoardInput input) {
			if (ModelState.IsValid)
				await BoardService.Create(input, ModelState);

			if (ModelState.IsValid)
				return RedirectToAction(nameof(Index));

			var viewModel = await BoardService.CreatePage(input);
			return View(viewModel);
		}
	}
}