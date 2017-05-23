using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Forum3.Services;
using InputModels = Forum3.Models.InputModels;
using PageViewModels = Forum3.Models.ViewModels.Roles.Pages;

namespace Forum3.Controllers {
	[Authorize]
	public class Roles : ForumController {
		RoleService RoleService { get; }

		public Roles(
			RoleService roleService,
			UserService userService
		) : base(userService) {
			RoleService = roleService;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = await RoleService.IndexPage();
			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Create() {
			var viewModel = new PageViewModels.CreatePage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(InputModels.CreateRoleInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleService.Create(input);
				ProcessServiceResponse(serviceResponse);

				if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
					return Redirect(serviceResponse.RedirectPath);
			}

			var viewModel = new PageViewModels.CreatePage() {
				Name = input.Name,
				Description = input.Description
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(string id) {
			var viewModel = await RoleService.EditPage(id);
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(InputModels.EditRoleInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await RoleService.Edit(input);
				ProcessServiceResponse(serviceResponse);

				if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
					return Redirect(serviceResponse.RedirectPath);
			}

			var viewModel = await RoleService.EditPage(input.Id);

			viewModel.Name = input.Name;
			viewModel.Description = input.Description;

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(string id) {
			if (ModelState.IsValid) {
				await RoleService.Delete(id);
			}

			return RedirectToAction(nameof(Roles.Index), nameof(Roles));
		}
	}
}