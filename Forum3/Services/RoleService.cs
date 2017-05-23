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

			var roles = await DbContext.Roles.ToListAsync();

			foreach (var role in roles) {
				var createdBy = await UserManager.FindByIdAsync(role.UserId);

				viewModel.Roles.Add(new ItemViewModels.IndexRole {
					Id = role.Id,
					Description = role.Description,
					Name = role.Name,
					CreatedBy = createdBy.DisplayName,
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

			var existingRole = await RoleManager.FindByNameAsync(input.Name);

			if (existingRole != null)
				serviceResponse.ModelErrors.Add(nameof(InputModels.CreateRoleInput.Name), "A role with this name already exists");

			if (serviceResponse.ModelErrors.Any())
				return serviceResponse;

			await CreateRecord(input);

			var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContextAccessor.ActionContext);
			serviceResponse.RedirectPath = urlHelper.Action(nameof(Roles.Index), nameof(Roles));

			return serviceResponse;
		}

		async Task CreateRecord(InputModels.CreateRoleInput input) {
			var record = new ApplicationRole {
				Name = input.Name,
				Description = input.Description,
				CreatedDate = DateTime.Now,
				UserId = UserService.ContextUser.ApplicationUser.Id
			};

			await RoleManager.CreateAsync(record);
		}
	}
}