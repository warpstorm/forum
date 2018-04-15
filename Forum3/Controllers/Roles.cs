using Forum3.Extensions;
using Forum3.Interfaces.Services;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Forum3.Controllers {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemViewModels = Models.ViewModels.Roles.Items;
	using PageViewModels = Models.ViewModels.Roles.Pages;

	[Authorize(Roles = "Admin")]
	public class Roles : Controller {
		RoleRepository RoleRepository { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }
		IForumViewResult ForumViewResult { get; }

		public Roles(
			RoleRepository roleRepository,
			UserManager<DataModels.ApplicationUser> userManager,
			RoleManager<DataModels.ApplicationRole> roleManager,
			IForumViewResult forumViewResult
		) {
			RoleRepository = roleRepository;
			UserManager = userManager;
			RoleManager = roleManager;
			ForumViewResult = forumViewResult;
		}


		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new PageViewModels.IndexPage();

			var roles = await RoleManager.Roles.OrderBy(r => r.Name).ToListAsync();

			foreach (var role in roles) {
				DataModels.ApplicationUser createdBy = null;
				DataModels.ApplicationUser modifiedBy = null;

				if (role.CreatedById != null)
					createdBy = await UserManager.FindByIdAsync(role.CreatedById);

				if (role.ModifiedById != null)
					modifiedBy = await UserManager.FindByIdAsync(role.ModifiedById);

				IList<DataModels.ApplicationUser> usersInRole = null;

				try {
					usersInRole = await UserManager.GetUsersInRoleAsync(role.Name);
				}
				catch (OperationCanceledException) {
					continue;
				}

				viewModel.Roles.Add(new ItemViewModels.IndexRole {
					Id = role.Id,
					Description = role.Description,
					Name = role.Name,
					CreatedBy = createdBy?.DisplayName,
					Created = role.CreatedDate.ToPassedTimeString(),
					ModifiedBy = modifiedBy?.DisplayName,
					Modified = role.ModifiedDate.ToPassedTimeString(),
					NumberOfUsers = usersInRole.Count()
				});
			}

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult Create() {
			var viewModel = new PageViewModels.CreatePage();
			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(InputModels.CreateRoleInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.Create(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = new PageViewModels.CreatePage() {
					Name = input.Name,
					Description = input.Description
				};

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, viewModel); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Edit(string id) {
			var viewModel = await GetEditPageModel(id);

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(InputModels.EditRoleInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.Edit(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = await GetEditPageModel(input.Id);

				viewModel.Name = input.Name;
				viewModel.Description = input.Description;

				return await Task.Run(() => { return ForumViewResult.ViewResult(this, viewModel); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Delete(string id) {
			if (ModelState.IsValid)
				await RoleRepository.Delete(id);

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public async Task<IActionResult> UserList(string id) {
			var viewModel = await RoleRepository.UserList(id);
			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> AddUser(string id, string user) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.AddUser(id, user);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = await GetEditPageModel(id);
				return await Task.Run(() => { return ForumViewResult.ViewResult(this, nameof(Edit), viewModel); });
			}
		}

		[HttpGet]
		public async Task<IActionResult> RemoveUser(string id, string user) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleRepository.RemoveUser(id, user);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				var viewModel = await GetEditPageModel(id);
				return await Task.Run(() => { return ForumViewResult.ViewResult(this, nameof(Edit), viewModel); });
			}
		}

		public async Task<PageViewModels.EditPage> GetEditPageModel(string id) {
			var role = await RoleManager.FindByIdAsync(id);

			if (role is null)
				throw new Exception($"A record does not exist with ID '{id}'");

			DataModels.ApplicationUser createdBy = null;
			DataModels.ApplicationUser modifiedBy = null;

			if (role.CreatedById != null)
				createdBy = await UserManager.FindByIdAsync(role.CreatedById);

			if (role.ModifiedById != null)
				modifiedBy = await UserManager.FindByIdAsync(role.ModifiedById);

			var usersInRole = await UserManager.GetUsersInRoleAsync(role.Name);

			var viewModel = new PageViewModels.EditPage {
				Id = role.Id,
				Description = role.Description,
				Name = role.Name,
				CreatedBy = createdBy?.DisplayName,
				Created = role.CreatedDate.ToPassedTimeString(),
				ModifiedBy = modifiedBy?.DisplayName,
				Modified = role.ModifiedDate.ToPassedTimeString(),
				NumberOfUsers = usersInRole.Count()
			};

			return viewModel;
		}
	}
}