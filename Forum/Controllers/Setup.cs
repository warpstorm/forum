using Forum.Contexts;
using Forum.Errors;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Forum.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Setup : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		SettingsRepository SettingsRepository { get; }
		SetupService SetupService { get; }
		IForumViewResult ForumViewResult { get; }
		IUrlHelper UrlHelper { get; }

		public Setup(
			ApplicationDbContext dbContext,
			UserContext userContext,
			SettingsRepository settingsRepository,
			AccountRepository accountRepository,
			SetupService setupService,
			IForumViewResult forumViewResult,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			SettingsRepository = settingsRepository;
			SetupService = setupService;
			ForumViewResult = forumViewResult;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public IActionResult Initialize() {
			CheckContext();

			var totalPages = 4;

			var viewModel = new ViewModels.Delay {
				ActionName = "Initializing",
				ActionNote = "Beginning the setup process",
				CurrentPage = 0,
				TotalPages = totalPages,
				NextAction = UrlHelper.Action(nameof(Setup.Process), nameof(Setup), new InputModels.Continue { CurrentStep = 1, TotalSteps = totalPages })
			};

			return ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		public async Task<IActionResult> Process(InputModels.Continue input) {
			CheckContext();
		
			var note = string.Empty;

			switch(input.CurrentStep) {
				case 1:
					note = "Roles have been setup.";
					await SetupService.SetupRoles();
					break;

				case 2:
					note = "Admins have been added.";
					await SetupService.SetupAdmins();
					break;

				case 3:
					note = "The first category has been added.";
					SetupService.SetupCategories();
					break;

				case 4:
					note = "The first board has been added.";
					SetupService.SetupBoards();
					break;
			}

			input.CurrentStep++;

			var viewModel = new ViewModels.Delay {
				ActionName = "Processing",
				ActionNote = note,
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Setup.Process), nameof(Setup), input)
			};

			if (input.CurrentStep > input.TotalSteps) {
				viewModel.NextAction = "/";
			}

			return ForumViewResult.ViewResult(this, "Delay", viewModel);
		}

		void CheckContext() {
			if (SettingsRepository.Installed()) {
				throw new HttpException("The forum has already been installed.");
			}

			if (!UserContext.IsAuthenticated) {
				throw new HttpException("You must create an account and log into it first.");
			}

			if (!UserContext.IsAdmin && DbContext.Users.Count() > 1) {
				throw new HttpException("Non-admins can only run this process when there's one user registered.");
			}
		}
	}
}
