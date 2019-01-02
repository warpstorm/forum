using Forum.Contexts;
using Forum.Controllers;
using Forum.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ItemViewModels = Models.ViewModels.Roles.Items;
	using PageViewModels = Models.ViewModels.Roles.Pages;
	using ServiceModels = Models.ServiceModels;

	public class RoleRepository {
		public List<DataModels.BoardRole> BoardRoles => _BoardRoles ?? (_BoardRoles = DbContext.BoardRoles.ToList());
		List<DataModels.BoardRole> _BoardRoles;

		public List<DataModels.ApplicationRole> SiteRoles => _SiteRoles ?? (_SiteRoles = DbContext.Roles.ToList());
		List<DataModels.ApplicationRole> _SiteRoles;

		public List<IdentityUserRole<string>> UserRoles => _UserRoles ?? (_UserRoles = DbContext.UserRoles.ToList());
		List<IdentityUserRole<string>> _UserRoles;

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IUrlHelper UrlHelper { get; }

		public RoleRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			RoleManager<DataModels.ApplicationRole> roleManager,
			SignInManager<DataModels.ApplicationUser> signInManager,
			UserManager<DataModels.ApplicationUser> userManager,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			RoleManager = roleManager;
			SignInManager = signInManager;
			UserManager = userManager;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public List<SelectListItem> PickList(int boardId) {
			var boardRolesQuery = from boardRole in BoardRoles
								  where boardRole.BoardId == boardId
								  select boardRole.RoleId;

			var selectedItems = boardRolesQuery.ToList();

			var pickList = new List<SelectListItem>();

			var roles = RoleManager.Roles.OrderBy(r => r.Name).ToList();

			foreach (var role in roles) {
				pickList.Add(new SelectListItem {
					Text = role.Name,
					Value = role.Id,
					Selected = selectedItems.Contains(role.Id)
				});
			}

			return pickList;
		}

		public async Task<ServiceModels.ServiceResponse> Create(InputModels.CreateRoleInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			if (input.Name != null) {
				input.Name = input.Name.Trim();
			}

			if (string.IsNullOrEmpty(input.Name)) {
				serviceResponse.Error(nameof(InputModels.CreateRoleInput.Name), "Name is required");
			}

			if (input.Description != null) {
				input.Description = input.Description.Trim();
			}

			if (string.IsNullOrEmpty(input.Description)) {
				serviceResponse.Error(nameof(InputModels.CreateRoleInput.Description), "Description is required");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			if (await RoleManager.FindByNameAsync(input.Name) != null) {
				serviceResponse.Error(nameof(InputModels.CreateRoleInput.Name), "A role with this name already exists");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			await CreateRecord(input);

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Roles.Index), nameof(Roles));

			return serviceResponse;
		}

		public async Task CreateRecord(InputModels.CreateRoleInput input) {
			var now = DateTime.Now;

			var record = new DataModels.ApplicationRole {
				Name = input.Name,
				Description = input.Description,
				CreatedDate = now,
				CreatedById = UserContext.ApplicationUser.Id,
				ModifiedDate = now,
				ModifiedById = UserContext.ApplicationUser.Id
			};

			await RoleManager.CreateAsync(record);
		}

		public async Task<ServiceModels.ServiceResponse> Edit(InputModels.EditRoleInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = await RoleManager.FindByIdAsync(input.Id);

			if (record is null) {
				serviceResponse.Error(nameof(InputModels.EditRoleInput.Id), $"A record does not exist with ID '{input.Id}'");
			}

			if (input.Name != null) {
				input.Name = input.Name.Trim();
			}

			if (string.IsNullOrEmpty(input.Name)) {
				serviceResponse.Error(nameof(InputModels.EditRoleInput.Name), "Name is required");
			}

			if (input.Description != null) {
				input.Description = input.Description.Trim();
			}

			if (string.IsNullOrEmpty(input.Description)) {
				serviceResponse.Error(nameof(InputModels.EditRoleInput.Description), "Description is required");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var existingRole = await RoleManager.FindByNameAsync(input.Name);

			if (existingRole != null && existingRole.Id != input.Id) {
				serviceResponse.Error(nameof(InputModels.EditRoleInput.Name), "A role with this name already exists");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var modified = false;

			if (record.Name != input.Name) {
				record.Name = input.Name;
				modified = true;
			}

			if (record.Description != input.Description) {
				record.Description = input.Description;
				modified = true;
			}

			if (modified) {
				record.ModifiedById = UserContext.ApplicationUser.Id;
				record.ModifiedDate = DateTime.Now;
				await RoleManager.UpdateAsync(record);
			}

			serviceResponse.RedirectPath = UrlHelper.Action(nameof(Roles.Index), nameof(Roles));

			return serviceResponse;
		}

		public async Task Delete(string id) {
			var record = await RoleManager.FindByIdAsync(id);

			if (record != null) {
				await RoleManager.DeleteAsync(record);
			}
		}

		public async Task<PageViewModels.UserListPage> UserList(string id) {
			var role = await RoleManager.FindByIdAsync(id);

			if (role is null) {
				throw new HttpNotFoundError();
			}

			var usersInRole = await UserManager.GetUsersInRoleAsync(role.Name);

			var userRecords = await UserManager.Users.OrderBy(r => r.DisplayName).Select(u => new ItemViewModels.UserListItem {
				Id = u.Id,
				Name = u.DisplayName
			}).ToListAsync();

			var existingUsers = new List<ItemViewModels.UserListItem>();

			foreach (var user in usersInRole) {
				existingUsers.Add(userRecords.Single(u => u.Id == user.Id));
			}

			var availableUsers = userRecords.Except(existingUsers).ToList();

			return new PageViewModels.UserListPage {
				Id = role.Id,
				Name = role.Name,
				ExistingUsers = existingUsers,
				AvailableUsers = availableUsers
			};
		}

		public async Task<ServiceModels.ServiceResponse> AddUser(string roleId, string userId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var roleRecord = await RoleManager.FindByIdAsync(roleId);

			if (roleRecord is null) {
				serviceResponse.Error($"A record does not exist with ID '{roleId}'");
			}

			var userRecord = await UserManager.FindByIdAsync(userId);

			if (userRecord is null) {
				serviceResponse.Error($"A record does not exist with ID '{roleId}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var result = await UserManager.AddToRoleAsync(userRecord, roleRecord.Name);

			if (result.Succeeded) {
				if (userId == UserContext.ApplicationUser.Id) {
					await SignInManager.SignOutAsync();
				}

				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Roles.Edit), nameof(Roles), new { Id = roleId });
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> RemoveUser(string roleId, string userId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var roleRecord = await RoleManager.FindByIdAsync(roleId);

			if (roleRecord is null) {
				serviceResponse.Error($"A record does not exist with ID '{roleId}'");
			}

			var userRecord = await UserManager.FindByIdAsync(userId);

			if (userRecord is null) {
				serviceResponse.Error($"A record does not exist with ID '{roleId}'");
			}

			if (!serviceResponse.Success) {
				return serviceResponse;
			}

			var result = await UserManager.RemoveFromRoleAsync(userRecord, roleRecord.Name);

			if (result.Succeeded) {
				if (userId == UserContext.ApplicationUser.Id) {
					await SignInManager.SignOutAsync();
				}

				serviceResponse.RedirectPath = UrlHelper.Action(nameof(Roles.Edit), nameof(Roles), new { Id = roleId });
			}

			return serviceResponse;
		}

		public bool CanAccessBoards(IEnumerable<DataModels.Board> boards) {
			var boardRoles = BoardRoles.Where(r => boards.Any(b => b.Id == r.BoardId)).Select(r => r.RoleId).ToList();

			if (!UserContext.IsAdmin && boardRoles.Any() && !boardRoles.Intersect(UserContext.Roles).Any()) {
				return false;
			}

			return true;
		}
	}
}