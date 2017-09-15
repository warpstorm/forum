using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Forum3.Services;
using Forum3.Annotations;
using Forum3.Models.InputModels;
using System.Linq;
using System.Collections.Generic;

namespace Forum3.Controllers {
	public class Smileys : ForumController {
		SmileyService SmileyService { get; }

		public Smileys(
			SmileyService smileyService
		) {
			SmileyService = smileyService;
		}

		public async Task<IActionResult> Index() {
			var viewModel = await SmileyService.IndexPage();
			return View(viewModel);
		}

		public IActionResult Create() {
			var viewModel = SmileyService.CreatePage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(CreateSmileyInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await SmileyService.Create(input);
				ProcessServiceResponse(serviceResponse);

				if (ModelState.IsValid) {
					if (!string.IsNullOrEmpty(serviceResponse.RedirectPath))
						return Redirect(serviceResponse.RedirectPath);

					return RedirectToReferrer();
				}
			}

			var viewModel = SmileyService.CreatePage(input);
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(EditSmileysInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await SmileyService.Edit(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectToReferrer();
		}

		public async Task<IActionResult> Delete(int id) {
			var serviceResponse = await SmileyService.Delete(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectToReferrer();
		}
	}
}