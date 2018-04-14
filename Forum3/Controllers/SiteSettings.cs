using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Extensions;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	[Authorize(Roles="Admin")]
	public class SiteSettings : ForumController {
		ApplicationDbContext DbContext { get; }
		SettingsRepository SettingsRepository { get; }

		public SiteSettings(
			ApplicationDbContext dbContext,
			SettingsRepository settingsRepository
		) {
			DbContext = dbContext;
			SettingsRepository = settingsRepository;
		}

		[HttpGet]
		public async Task<IActionResult> Index() {
			var settingNames = typeof(Constants.Settings).GetConstants();

			var settingsRecords = await DbContext.SiteSettings.Where(record => string.IsNullOrEmpty(record.UserId)).ToListAsync();

			var settingsList = new List<ViewModels.IndexItem>();

			foreach (var item in settingNames) {
				var existingRecord = settingsRecords.FirstOrDefault(record => record.Name == item);

				settingsList.Add(new ViewModels.IndexItem {
					Key = item,
					Value = existingRecord?.Value ?? string.Empty,
					AdminOnly = existingRecord?.AdminOnly ?? false,
				});
			}

			var viewModel = new ViewModels.IndexPage {
				Settings = settingsList
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public IActionResult Edit(InputModels.EditSettingsInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = SettingsRepository.Update(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}
	}
}