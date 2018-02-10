using Forum3.Models.InputModels;
using Forum3.Services.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Controllers {
	public class Boards : ForumController {
		BoardService BoardService { get; }

		public Boards(
			BoardService boardService
		) {
			BoardService = boardService;
		}

		[HttpGet]
		public IActionResult Index() {
			var viewModel = BoardService.IndexPage();
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Manage() {
			var viewModel = BoardService.ManagePage();
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Create() {
			var viewModel = BoardService.CreatePage();
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult Create(CreateBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardService.Create(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = BoardService.CreatePage(input);
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Edit(int id) {
			var viewModel = BoardService.EditPage(id);
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult Edit(EditBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardService.Edit(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = BoardService.EditPage(input.Id, input);
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult MoveCategoryUp(int id) {
			var serviceResponse = BoardService.MoveCategoryUp(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult MoveBoardUp(int id) {
			var serviceResponse = BoardService.MoveBoardUp(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult MergeCategory(MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardService.MergeCategory(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult MergeBoard(MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardService.MergeBoard(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}
	}
}