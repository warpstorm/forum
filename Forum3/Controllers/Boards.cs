using Forum3.Models.InputModels;
using Forum3.Processes.Boards;
using Forum3.ViewModelProviders.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Controllers {
	public class Boards : ForumController {
		[HttpGet]
		public IActionResult Index(
			[FromServices] IndexPage pageProvider
		) {
			var viewModel = pageProvider.Generate();
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Manage(
			[FromServices] ManagePage pageProvider
		) {
			var viewModel = pageProvider.Generate();
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Create(
			[FromServices] CreatePage pageProvider
		) {
			var viewModel = pageProvider.Generate();
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult Create(
			[FromServices] CreatePage pageProvider,
			[FromServices] CreateBoard process,
			CreateBoardInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = process.Execute(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = pageProvider.Generate(input);
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Edit(
			[FromServices] EditPage pageProvider,
			int id
		) {
			var viewModel = pageProvider.Generate(id);
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult Edit(
			[FromServices] EditPage pageProvider,
			[FromServices] EditBoard process,
			EditBoardInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = process.Execute(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = pageProvider.Generate(input.Id, input);
			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult MoveCategoryUp(
			[FromServices] MoveCategoryUp process,
			int id
		) {
			var serviceResponse = process.Execute(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult MoveBoardUp(
			[FromServices] MoveBoardUp process,
			int id
		) {
			var serviceResponse = process.Execute(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult MergeCategory(
			[FromServices] MergeCategory process,
			MergeInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = process.Execute(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult MergeBoard(
			[FromServices] MergeBoard process,
			MergeInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = process.Execute(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}
	}
}