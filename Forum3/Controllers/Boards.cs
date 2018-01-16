using Forum3.Models.InputModels;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	public class Boards : ForumController {
		BoardService BoardService { get; }

		public Boards(
			BoardService boardService
		) {
			BoardService = boardService;
		}

		[HttpGet]
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
		public async Task<IActionResult> Create(CreateBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardService.Create(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = await BoardService.CreatePage(input);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var viewModel = await BoardService.EditPage(id);
			return View(viewModel);
		}

		[HttpPost]
		public async Task<IActionResult> Edit(EditBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardService.Edit(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = await BoardService.EditPage(input.Id, input);
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult MoveCategoryUp(int id) {
			var serviceResponse = BoardService.MoveCategoryUp(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpGet]
		public async Task<IActionResult> MoveBoardUp(int id) {
			var serviceResponse = await BoardService.MoveBoardUp(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpPost]
		public async Task<IActionResult> MergeCategory(MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardService.MergeCategory(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}

		[HttpPost]
		public async Task<IActionResult> MergeBoard(MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardService.MergeBoard(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}
	}
}