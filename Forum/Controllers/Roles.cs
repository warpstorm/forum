using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Models.Errors;
using Forum.Services;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	[Authorize(Roles = Constants.InternalKeys.Admin)]
	public class Roles : Controller {
		RoleRepository RoleRepository { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }
		ForumViewResult ForumViewResult { get; }

		public Roles(
			RoleRepository roleRepository,
			UserManager<DataModels.ApplicationUser> userManager,
			RoleManager<DataModels.ApplicationRole> roleManager,
			ForumViewResult forumViewResult
		) {
			RoleRepository = roleRepository;
			UserManager = userManager;
			RoleManager = roleManager;
			ForumViewResult = forumViewResult;
		}


		[ActionLog]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new ViewModels.Roles.IndexPage();

			var roles = await RoleManager.Roles.OrderBy(r => r.Name).ToListAsync();

			foreach (var role in roles) {
				DataModels.ApplicationUser createdBy = null;
				DataModels.ApplicationUser modifiedBy = null;

				if (role.CreatedById != null) {
					createdBy = await UserManager.FindByIdAsync(role.CreatedById);
				}

				if (role.ModifiedById != null) {
					modifiedBy = await UserManager.FindByIdAsync(role.ModifiedById);
				}

				IList<DataModels.ApplicationUser> usersInRole = null;

				try {
					usersInRole = await UserManager.GetUsersInRoleAsync(role.Name);
				}
				catch (OperationCanceledException) {
					continue;
				}

				viewModel.Roles.Add(new ViewModels.Roles.IndexRole {
					Id = role.Id,
					Description = role.Description,
					Name = role.Name,
					CreatedBy = createdBy?.DecoratedName,
					Created = role.CreatedDate.ToPassedTimeString(),
					ModifiedBy = modifiedBy?.DecoratedName,
					Modified = role.ModifiedDate.ToPassedTimeString(),
					NumberOfUsers = usersInRole.Count()
				});
			}

			return View(viewModel);
		}

		[ActionLog]
		[HttpGet]
		public IActionResult Create() {
			var viewModel = new ViewModels.Roles.CreatePage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(InputModels.CreateRoleInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.Create(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				var viewModel = new ViewModels.Roles.CreatePage() {
					Name = input.Name,
					Description = input.Description
				};

				return View(viewModel);
			}
		}

		[ActionLog]
		[HttpGet]
		public IActionResult Edit(string id) {
			var viewModel = GetEditPageModel(id);
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(InputModels.EditRoleInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.Edit(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				var viewModel = GetEditPageModel(input.Id);

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				return View(viewModel);
			}
		}

		[HttpGet]
		public async Task<IActionResult> Delete(string id) {
			if (ModelState.IsValid) {
				await RoleRepository.Delete(id);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[ActionLog]
		[HttpGet]
		public async Task<IActionResult> UserList(string id) {
			var viewModel = await RoleRepository.UserList(id);
			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> AddUser(string id, string user) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.AddUser(id, user);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				var viewModel = GetEditPageModel(id);
				return View(nameof(Edit), viewModel);
			}
		}

		[HttpGet]
		public async Task<IActionResult> RemoveUser(string id, string user) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.RemoveUser(id, user);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, failSync: FailureCallback);
			}

			return FailureCallback();

			IActionResult FailureCallback() {
				var viewModel = GetEditPageModel(id);
				return View(nameof(Edit), viewModel);
			}
		}

		public ViewModels.Roles.EditPage GetEditPageModel(string id) {
			var role = RoleManager.FindByIdAsync(id).Result;

			if (role is null) {
				throw new HttpNotFoundError();
			}

			DataModels.ApplicationUser createdBy = null;
			DataModels.ApplicationUser modifiedBy = null;

			if (role.CreatedById != null) {
				createdBy = UserManager.FindByIdAsync(role.CreatedById).Result;
			}

			if (role.ModifiedById != null) {
				modifiedBy = UserManager.FindByIdAsync(role.ModifiedById).Result;
			}

			var usersInRole = UserManager.GetUsersInRoleAsync(role.Name).Result;

			var viewModel = new ViewModels.Roles.EditPage {
				Id = role.Id,
				Description = role.Description,
				Name = role.Name,
				CreatedBy = createdBy?.DecoratedName,
				Created = role.CreatedDate,
				ModifiedBy = modifiedBy?.DecoratedName,
				Modified = role.ModifiedDate,
				NumberOfUsers = usersInRole.Count()
			};

			return viewModel;
		}
	}
}