using Forum3.Contexts;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using PageViewModels = Models.ViewModels.Boards.Pages;

	public class Boards : ForumController {
		ApplicationDbContext DbContext { get; }
		BoardRepository BoardRepository { get; }
		CategoryRepository CategoryRepository { get; }
		RoleRepository RoleRepository { get; }
		UserRepository UserRepository { get; }
		NotificationRepository NotificationRepository { get; }

		public Boards(
			ApplicationDbContext dbContext,
			BoardRepository boardRepository,
			CategoryRepository categoryRepository,
			RoleRepository roleRepository,
			UserRepository userRepository,
			NotificationRepository notificationRepository
		) {
			DbContext = dbContext;
			BoardRepository = boardRepository;
			CategoryRepository = categoryRepository;
			RoleRepository = roleRepository;
			UserRepository = userRepository;
			NotificationRepository = notificationRepository;
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

			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Manage() {
			var viewModel = new PageViewModels.IndexPage {
				Categories = CategoryRepository.Index()
			};

			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Create() {
			var viewModel = new PageViewModels.CreatePage() {
				Categories = CategoryRepository.PickList()
			};

			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult Create(InputModels.CreateBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.Add(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = new PageViewModels.CreatePage() {
				Categories = CategoryRepository.PickList()
			};

			viewModel.Name = input.Name;
			viewModel.Description = input.Description;

			if (!string.IsNullOrEmpty(input.Category))
				viewModel.Categories.First(item => item.Value == input.Category).Selected = true;

			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult Edit(int id) {
			var boardRecord = DbContext.Boards.FirstOrDefault(b => b.Id == id);

			if (boardRecord is null)
				throw new Exception($"A record does not exist with ID '{id}'");

			var viewModel = new PageViewModels.EditPage {
				Id = boardRecord.Id,
				Categories = CategoryRepository.PickList(),
				Roles = RoleRepository.PickList(boardRecord.Id)
			};

			var category = DbContext.Categories.Find(boardRecord.CategoryId);

			viewModel.Name = boardRecord.Name;
			viewModel.Description = boardRecord.Description;
			viewModel.Categories.First(item => item.Text == category.Name).Selected = true;

			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult Edit(InputModels.EditBoardInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.Update(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var boardRecord = DbContext.Boards.FirstOrDefault(b => b.Id == input.Id);

			if (boardRecord is null)
				throw new Exception($"A record does not exist with ID '{input.Id}'");

			var viewModel = new PageViewModels.EditPage {
				Id = boardRecord.Id,
				Categories = CategoryRepository.PickList(),
				Roles = RoleRepository.PickList(boardRecord.Id)
			};

			viewModel.Name = input.Name;
			viewModel.Description = input.Description;

			if (!string.IsNullOrEmpty(input.Category))
				viewModel.Categories.First(item => item.Value == input.Category).Selected = true;

			return View(viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult MoveCategoryUp(
			int id
		) {
			var serviceResponse = CategoryRepository.MoveUp(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult MoveBoardUp(
			int id
		) {
			var serviceResponse = BoardRepository.MoveUp(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult MergeCategory(
			InputModels.MergeInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = CategoryRepository.Merge(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}

		[Authorize(Roles="Admin")]
		[HttpPost]
		public IActionResult MergeBoard(
			InputModels.MergeInput input
		) {
			if (ModelState.IsValid) {
				var serviceResponse = BoardRepository.Merge(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}
	}
}