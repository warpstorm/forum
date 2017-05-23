using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Forum3.Data;
using Forum3.Models.DataModels;
using Forum3.Models.ServiceModels;
using Forum3.Controllers;
using DataModels = Forum3.Models.DataModels;
using InputModels = Forum3.Models.InputModels;
using PageViewModels = Forum3.Models.ViewModels.Roles.Pages;
using ItemViewModels = Forum3.Models.ViewModels.Roles.Items;
using System;
using Forum3.Helpers;

namespace Forum3.Services {
	public class RoleService {
		ApplicationDbContext DbContext { get; }
		UserService UserService { get; set; }
		UserManager<ApplicationUser> UserManager { get; }
		RoleManager<ApplicationRole> RoleManager { get; }
		IUrlHelperFactory UrlHelperFactory { get; }
		IActionContextAccessor ActionContextAccessor { get; }

		public RoleService(
			ApplicationDbContext dbContext,
			UserService userService,
			UserManager<ApplicationUser> userManager,
			RoleManager<ApplicationRole> roleManager,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserService = userService;
			UserManager = userManager;
			RoleManager = roleManager;
			ActionContextAccessor = actionContextAccessor;
			UrlHelperFactory = urlHelperFactory;
		}

		public async Task<PageViewModels.IndexPage> IndexPage() {
			var viewModel = new PageViewModels.IndexPage();

			var roles = await DbContext.Roles.OrderBy(r => r.Name).ToListAsync();

			foreach (var role in roles) {
				ApplicationUser createdBy = null;
				ApplicationUser modifiedBy = null;

				if (role.CreatedById != null)
					createdBy = await UserManager.FindByIdAsync(role.CreatedById);

				if (role.ModifiedById != null)
					modifiedBy = await UserManager.FindByIdAsync(role.ModifiedById);

				viewModel.Roles.Add(new ItemViewModels.IndexRole {
					Id = role.Id,
					Description = role.Description,
					Name = role.Name,
					CreatedBy = createdBy?.DisplayName,
					Created = role.CreatedDate.ToPassedTimeString(),
					ModifiedBy = modifiedBy?.DisplayName,
					Modified = role.ModifiedDate.ToPassedTimeString(),
					NumberOfUsers = role.Users.Count
				});
			}

			return viewModel;
		}

		public async Task<ServiceResponse> Create(InputModels.CreateRoleInput input) {
			var serviceResponse = new ServiceResponse();

			if (input.Name != null)
				input.Name = input.Name.Trim();

			if (string.IsNullOrEmpty(input.Name))
				serviceResponse.ModelErrors.Add(nameof(InputModels.CreateRoleInput.Name), "Name is required");

			if (input.Description != null)
				input.Description = input.Description.Trim();

			if (string.IsNullOrEmpty(input.Description))
				serviceResponse.ModelErrors.Add(nameof(InputModels.CreateRoleInput.Description), "Description is required");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			if (await DbContext.Roles.AnyAsync(r => r.Name == input.Name))
				serviceResponse.ModelErrors.Add(nameof(InputModels.CreateRoleInput.Name), "A role with this name already exists");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			await CreateRecord(input);

			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);
			serviceResponse.RedirectPath = urlHelper.Action(nameof(Roles.Index), nameof(Roles));

			return serviceResponse;
		}

		public async Task<PageViewModels.EditPage> EditPage(string id) {
			var role = await RoleManager.FindByIdAsync(id);

			ApplicationUser createdBy = null;
			ApplicationUser modifiedBy = null;

			if (role.CreatedById != null)
				createdBy = await UserManager.FindByIdAsync(role.CreatedById);

			if (role.ModifiedById != null)
				modifiedBy = await UserManager.FindByIdAsync(role.ModifiedById);

			return new PageViewModels.EditPage {
				Id = role.Id,
				Description = role.Description,
				Name = role.Name,
				CreatedBy = createdBy?.DisplayName,
				Created = role.CreatedDate.ToPassedTimeString(),
				ModifiedBy = modifiedBy?.DisplayName,
				Modified = role.ModifiedDate.ToPassedTimeString(),
				NumberOfUsers = role.Users.Count
			};
		}

		public async Task<ServiceResponse> Edit(InputModels.EditRoleInput input) {
			var serviceResponse = new ServiceResponse();

			var record = await RoleManager.FindByIdAsync(input.Id);

			if (record == null)
				serviceResponse.ModelErrors.Add(nameof(InputModels.EditRoleInput.Id), $"A record does not exist with ID '{input.Id}'");

			if (input.Name != null)
				input.Name = input.Name.Trim();

			if (string.IsNullOrEmpty(input.Name))
				serviceResponse.ModelErrors.Add(nameof(InputModels.EditRoleInput.Name), "Name is required");

			if (input.Description != null)
				input.Description = input.Description.Trim();

			if (string.IsNullOrEmpty(input.Description))
				serviceResponse.ModelErrors.Add(nameof(InputModels.EditRoleInput.Description), "Description is required");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			if (await DbContext.Roles.AnyAsync(r => r.Id != input.Id && r.Name == input.Name))
				serviceResponse.ModelErrors.Add(nameof(InputModels.EditRoleInput.Name), "A role with this name already exists");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			if (record.Name != input.Name) {
				record.Name = input.Name;
				DbContext.Entry(record).State = EntityState.Modified;
			}

			if (record.Description != input.Description) {
				record.Description = input.Description;
				DbContext.Entry(record).State = EntityState.Modified;
			}

			if (DbContext.Entry(record).State == EntityState.Modified) {
				record.ModifiedById = UserService.ContextUser.ApplicationUser.Id;
				record.ModifiedDate = DateTime.Now;
				await DbContext.SaveChangesAsync();
			}

			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);
			serviceResponse.RedirectPath = urlHelper.Action(nameof(Roles.Index), nameof(Roles));

			return serviceResponse;
		}

		public async Task Delete(string id) {
			var record = await RoleManager.FindByIdAsync(id);

			if (record != null)
				await RoleManager.DeleteAsync(record);
		}

		async Task CreateRecord(InputModels.CreateRoleInput input) {
			var now = DateTime.Now;

			var record = new ApplicationRole {
				Name = input.Name,
				Description = input.Description,
				CreatedDate = now,
				CreatedById = UserService.ContextUser.ApplicationUser.Id,
				ModifiedDate = now,
				ModifiedById = UserService.ContextUser.ApplicationUser.Id
			};

			await RoleManager.CreateAsync(record);
		}
	}
}