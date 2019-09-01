using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Boards : Controller {
		BoardRepository BoardRepository { get; }
		RoleRepository RoleRepository { get; }

		public Boards(
			BoardRepository boardRepository,
			RoleRepository roleRepository
		) {
			BoardRepository = boardRepository;
			RoleRepository = roleRepository;
		}

		[ActionLog("is viewing the board index.")]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new ViewModels.Boards.IndexPage {
				Categories = await BoardRepository.CategoryIndex(true)
			};

			if (!viewModel.Categories.Any()) {
				return RedirectToAction(nameof(Administration.Install), nameof(Administration));
			}

			return View(viewModel);
		}

		[ActionLog("is managing the board index.")]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Manage() {
			var viewModel = new ViewModels.Boards.IndexPage {
				Categories = await BoardRepository.CategoryIndex()
			};

			return View(viewModel);
		}

		[ActionLog("is creating a board.")]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Create() {
			var viewModel = new ViewModels.Boards.CreatePage() {
				Categories = await BoardRepository.CategoryPickList()
			};

			return View(viewModel);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> Create(InputModels.CreateBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.AddBoard(input);
				return await this.RedirectFromService(serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = new ViewModels.Boards.CreatePage() {
					Categories = await BoardRepository.CategoryPickList()
				};

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category)) {
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
				}

				return View(viewModel);
			}
		}

		[ActionLog("is editing a board.")]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var boardRecord = (await BoardRepository.Records()).First(b => b.Id == id);
			var category = (await BoardRepository.Categories()).First(item => item.Id == boardRecord.CategoryId);

			var viewModel = new ViewModels.Boards.EditPage {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				Categories = await BoardRepository.CategoryPickList(),
				Roles = await RoleRepository.PickList(boardRecord.Id),
			};

			viewModel.Categories.First(item => item.Text == category.Name).Selected = true;

			return View(viewModel);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> Edit(InputModels.EditBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.UpdateBoard(input);
				return await this.RedirectFromService(serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var boardRecord = (await BoardRepository.Records()).First(b => b.Id == input.Id);

				var viewModel = new ViewModels.Boards.EditPage {
					Id = boardRecord.Id,
					Categories = await BoardRepository.CategoryPickList(),
					Roles = await RoleRepository.PickList(boardRecord.Id)
				};

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category)) {
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
				}

				return View(viewModel);
			}
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> MoveCategoryUp(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MoveCategoryUp(id);
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> MoveBoardUp(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MoveBoardUp(id);
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> MergeCategory(InputModels.MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MergeCategory(input);
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> MergeBoard(InputModels.MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MergeBoard(input);
				return await this.RedirectFromService(serviceResponse);
			}

			return this.RedirectToReferrer();
		}
	}
}