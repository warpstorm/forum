using Forum3.Contexts;
using Forum3.Exceptions;
using Forum3.Interfaces.Services;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using PageViewModels = Models.ViewModels.Boards.Pages;

	public class Boards : Controller {
		ApplicationDbContext DbContext { get; }
		BoardRepository BoardRepository { get; }
		CategoryRepository CategoryRepository { get; }
		RoleRepository RoleRepository { get; }
		UserRepository UserRepository { get; }
		NotificationRepository NotificationRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Boards(
			ApplicationDbContext dbContext,
			BoardRepository boardRepository,
			CategoryRepository categoryRepository,
			RoleRepository roleRepository,
			UserRepository userRepository,
			NotificationRepository notificationRepository,
			IForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			BoardRepository = boardRepository;
			CategoryRepository = categoryRepository;
			RoleRepository = roleRepository;
			UserRepository = userRepository;
			NotificationRepository = notificationRepository;
			ForumViewResult = forumViewResult;
		}

		[HttpGet]
		public IActionResult Index() {
			var birthdays = UserRepository.GetBirthdaysList();
			var onlineUsers = UserRepository.GetOnlineList();
			var notifications = NotificationRepository.Index();

			var viewModel = new PageViewModels.IndexPage {
				Birthdays = birthdays.ToArray(),
				Categories = CategoryRepository.Index(),
				OnlineUsers = onlineUsers,
				Notifications = notifications
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Manage() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = CategoryRepository.Index()
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Create() {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = CategoryRepository.PickList()
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Create(InputModels.CreateBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.Add(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = new PageViewModels.CreatePage() {
					Categories = CategoryRepository.PickList()
				};

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, viewModel); });
			}
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Edit(int id) {
			var boardRecord = BoardRepository.FirstOrDefault(b => b.Id == id);

			if (boardRecord is null)
				throw new HttpNotFoundException($"A record does not exist with ID '{id}'");

			var viewModel = new PageViewModels.EditPage {
				Id = boardRecord.Id,
				Categories = CategoryRepository.PickList(),
				Roles = RoleRepository.PickList(boardRecord.Id)
			};

			var category = DbContext.Categories.Find(boardRecord.CategoryId);

			viewModel.Name = boardRecord.Name;
			viewModel.Description = boardRecord.Description;
			viewModel.Categories.First(item => item.Text == category.Name).Selected = true;

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Edit(InputModels.EditBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.Update(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var boardRecord = BoardRepository.FirstOrDefault(b => b.Id == input.Id);

				if (boardRecord is null)
					throw new HttpNotFoundException($"A record does not exist with ID '{input.Id}'");

				var viewModel = new PageViewModels.EditPage {
					Id = boardRecord.Id,
					Categories = CategoryRepository.PickList(),
					Roles = RoleRepository.PickList(boardRecord.Id)
				};

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				if (!string.IsNullOrEmpty(input.Category))
					viewModel.Categories.First(item => item.Value == input.Category).Selected = true;

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, viewModel); });
			}
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public async Task<IActionResult> MoveCategoryUp(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = CategoryRepository.MoveUp(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public async Task<IActionResult> MoveBoardUp(int id) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.MoveUp(id);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public async Task<IActionResult> MergeCategory(
			InputModels.MergeInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = CategoryRepository.Merge(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public async Task<IActionResult> MergeBoard(InputModels.MergeInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.Merge(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}
	}
}