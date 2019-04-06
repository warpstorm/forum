using Forum.Controllers.Annotations;
using Forum.Services;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using PageViewModels = Models.ViewModels.Boards.Pages;

	public class Boards : Controller {
		BoardRepository BoardRepository { get; }
		RoleRepository RoleRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Boards(
			BoardRepository boardRepository,
			RoleRepository roleRepository,
			IForumViewResult forumViewResult
		) {
			BoardRepository = boardRepository;
			RoleRepository = roleRepository;
			ForumViewResult = forumViewResult;
		}

		[ActionLog("is viewing the board index.")]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = await BoardRepository.CategoryIndex(true)
			};

			if (!viewModel.Categories.Any()) {
				return RedirectToAction(nameof(Administration.Install), nameof(Administration));
			}

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[ActionLog("is managing the board index.")]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Manage() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = await BoardRepository.CategoryIndex()
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[ActionLog("is creating a board.")]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Create() {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = await BoardRepository.CategoryPickList()
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> Create(InputModels.CreateBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.AddBoard(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = new PageViewModels.CreatePage() {
					Categories = await BoardRepository.CategoryPickList()
				};

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category)) {
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
				}

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[ActionLog("is editing a board.")]
		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var boardRecord = (await BoardRepository.Records()).First(b => b.Id == id);
			var category = (await BoardRepository.Categories()).First(item => item.Id == boardRecord.CategoryId);

			var viewModel = new PageViewModels.EditPage {
				Id = boardRecord.Id,
				Name = boardRecord.Name,
				Description = boardRecord.Description,
				Categories = await BoardRepository.CategoryPickList(),
				Roles = await RoleRepository.PickList(boardRecord.Id),
			};

			viewModel.Categories.First(item => item.Text == category.Name).Selected = true;

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> Edit(InputModels.EditBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.UpdateBoard(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var boardRecord = (await BoardRepository.Records()).First(b => b.Id == input.Id);

				var viewModel = new PageViewModels.EditPage {
					Id = boardRecord.Id,
					Categories = await BoardRepository.CategoryPickList(),
					Roles = await RoleRepository.PickList(boardRecord.Id)
				};

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category)) {
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;
				}

				return await ForumViewResult.ViewResult(this, viewModel);
			}
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> MoveCategoryUp(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MoveCategoryUp(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpGet]
		public async Task<IActionResult> MoveBoardUp(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MoveBoardUp(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> MergeCategory(InputModels.MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MergeCategory(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[Authorize(Roles = Constants.InternalKeys.Admin)]
		[HttpPost]
		public async Task<IActionResult> MergeBoard(InputModels.MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await BoardRepository.MergeBoard(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}
	}
}