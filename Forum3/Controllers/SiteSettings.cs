using Forum3.Annotations;
using Forum3.Extensions;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels.SiteSettings;

	[Authorize(Roles="Admin")]
	public class SiteSettings : ForumController {
		SettingsRepository SettingsRepository { get; }

		public SiteSettings(
			SettingsRepository settingsRepository
		) {
			SettingsRepository = settingsRepository;
		}

		[HttpGet]
		public IActionResult Index() {
			var viewModel = new ViewModels.IndexPage();

			var settingNames = typeof(Constants.Settings).GetConstants();

			foreach (var settingName in settingNames) {
				var settingValue = SettingsRepository.GetSetting(settingName);
				viewModel.Settings.Add(new KeyValuePair<string, string>(settingName, settingValue));
			}

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