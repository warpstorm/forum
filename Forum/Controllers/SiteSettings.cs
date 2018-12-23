using Forum.Annotations;
using Forum.Contexts;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	[Authorize(Roles = Constants.InternalKeys.Admin)]
	public class SiteSettings : Controller {
		ApplicationDbContext DbContext { get; }
		SettingsRepository SettingsRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public SiteSettings(
			ApplicationDbContext dbContext,
			SettingsRepository settingsRepository,
			IForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			SettingsRepository = settingsRepository;
			ForumViewResult = forumViewResult;
		}

		[HttpGet]
		public IActionResult Index() {
			var settings = new BaseSettings();

			var settingsRecords = SettingsRepository.Where(record => string.IsNullOrEmpty(record.UserId)).ToList();

			var settingsList = new List<ViewModels.IndexItem>();

			foreach (var item in settings) {
				var existingRecord = settingsRecords.FirstOrDefault(record => record.Name == item.Key);

				var options = new List<SelectListItem>();
				var value = existingRecord?.Value ?? string.Empty;

				if (item.Options != null) {
					foreach (var option in item.Options) {
						options.Add(new SelectListItem {
							Text = option,
							Value = option,
							Selected = option == value
						});
					}
				}

				settingsList.Add(new ViewModels.IndexItem {
					Key = item.Key,
					Display = item.Display,
					Description = item.Description,
					Options = options,
					Value = value,
					AdminOnly = existingRecord?.AdminOnly ?? false,
				});
			}

			var viewModel = new ViewModels.IndexPage {
				Settings = settingsList
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(InputModels.EditSettingsInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = SettingsRepository.UpdateSiteSettings(input);
				return await ForumViewResult.RedirectFromService(this, serviceResponse, FailureCallback);
			}

			return await FailureCallback();

			async Task<IActionResult> FailureCallback() {
				return await Task.Run(() => { return ForumViewResult.RedirectToReferrer(this); });
			}
		}
	}
}