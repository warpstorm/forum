using Forum.Contracts;
using Forum.Controllers.Annotations;
using Forum.Data.Contexts;
using Forum.Extensions;
using Forum.Models.ServiceModels;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ControllerModels = Models.ControllerModels;
	using DataModels = Data.Models;
	using ViewModels = Models.ViewModels.Smileys;

	[Authorize(Roles = Constants.InternalKeys.Admin)]
	public class Smileys : Controller {
		ApplicationDbContext DbContext { get; }
		SmileyRepository SmileyRepository { get; }
		IImageStore ImageStore { get; }

		public Smileys(
			ApplicationDbContext dbContext,
			SmileyRepository smileyRepository,
			IImageStore imageStore
		) {
			DbContext = dbContext;
			SmileyRepository = smileyRepository;
			ImageStore = imageStore;
		}

		[ActionLog]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var viewModel = new ViewModels.IndexPage();

			foreach (var smiley in await SmileyRepository.Records()) {
				var sortColumn = smiley.SortOrder / 1000;
				var sortRow = smiley.SortOrder % 1000;

				viewModel.Smileys.Add(new ViewModels.IndexItem {
					Id = smiley.Id,
					Code = smiley.Code,
					Path = smiley.Path,
					Thought = smiley.Thought,
					Column = sortColumn,
					Row = sortRow
				});
			}

			return View(viewModel);
		}

		[ActionLog]
		[HttpGet]
		public IActionResult Create() {
			var viewModel = new ViewModels.CreatePage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(ControllerModels.Smileys.CreateSmileyInput input) {
			if (ModelState.IsValid) {
				var allowedExtensions = new[] { "gif", "png" };
				var extension = Path.GetExtension(input.File.FileName).ToLower().Substring(1);

				if (Regex.IsMatch(input.File.FileName, @"[^a-zA-Z 0-9_\-\.]")) {
					ModelState.AddModelError("File", "Your filename contains invalid characters.");
				}

				if (!allowedExtensions.Contains(extension)) {
					ModelState.AddModelError("File", $"Your file must be: {string.Join(", ", allowedExtensions)}.");
				}

				if (!(await SmileyRepository.FindByCode(input.Code) is null)) {
					ModelState.AddModelError(nameof(input.Code), "Another smiley exists with that code.");
				}

				if (ModelState.IsValid) {
					var smileyRecord = new DataModels.Smiley {
						Code = input.Code,
						Thought = input.Thought,
						FileName = input.File.FileName
					};

					DbContext.Smileys.Add(smileyRecord);

					using (var inputStream = input.File.OpenReadStream()) {
						smileyRecord.Path = await ImageStore.Save(new ImageStoreSaveOptions {
							ContainerName = Constants.InternalKeys.SmileyImageContainer,
							FileName = input.File.FileName,
							ContentType = input.File.ContentType,
							InputStream = inputStream,
							Overwrite = true
						});
					}

					await DbContext.SaveChangesAsync();

					TempData[Constants.InternalKeys.StatusMessage] = $"Smiley '{input.File.FileName}' was added with code '{input.Code}'.";

					var referrer = this.GetReferrer();
					return Redirect(referrer);
				}
			}

			var viewModel = new ViewModels.CreatePage {
				Code = input.Code,
				Thought = input.Thought
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(ControllerModels.Smileys.EditSmileysInput input) {
			if (ModelState.IsValid) {
				var smileySortOrder = new Dictionary<int, int>();

				foreach (var smileyInput in input.Smileys) {
					var smileyRecord = await SmileyRepository.FindById(smileyInput.Id);

					if (smileyRecord is null) {
						ModelState.AddModelError(nameof(smileyInput.Id), $@"No smiley was found with the id '{smileyInput.Id}'");
						break;
					}

					smileySortOrder.Add(smileyRecord.Id, smileyRecord.SortOrder);
				}

				if (ModelState.IsValid) {
					foreach (var smileyInput in input.Smileys) {
						var newSortOrder = (smileyInput.Column * 1000) + smileyInput.Row;

						if (smileySortOrder[smileyInput.Id] != newSortOrder) {
							foreach (var kvp in smileySortOrder.Where(kvp => smileyInput.Column == kvp.Value / 1000 && kvp.Value >= newSortOrder).ToList()) {
								smileySortOrder[kvp.Key]++;
							}

							smileySortOrder[smileyInput.Id] = newSortOrder;
						}
					}

					foreach (var smileyInput in input.Smileys) {
						var smileyRecord = await SmileyRepository.FindById(smileyInput.Id);

						if (smileyRecord.Code != smileyInput.Code) {
							smileyRecord.Code = smileyInput.Code;
							DbContext.Update(smileyRecord);
						}

						if (smileyRecord.Thought != smileyInput.Thought) {
							smileyRecord.Thought = smileyInput.Thought;
							DbContext.Update(smileyRecord);
						}

						if (smileyRecord.SortOrder != smileySortOrder[smileyRecord.Id]) {
							smileyRecord.SortOrder = smileySortOrder[smileyRecord.Id];
							DbContext.Update(smileyRecord);
						}
					}

					await DbContext.SaveChangesAsync();

					TempData[Constants.InternalKeys.StatusMessage] = $"Smileys were updated.";

					var referrer = this.GetReferrer();
					return Redirect(referrer);
				}
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			if (ModelState.IsValid) {
				var smileyRecord = await SmileyRepository.FindById(id);

				if (smileyRecord is null) {
					TempData[Constants.InternalKeys.StatusMessage] = $"No smiley was found with the id '{id}'";
					return this.RedirectToReferrer();
				}

				DbContext.Smileys.Remove(smileyRecord);

				var thoughts = DbContext.MessageThoughts.Where(t => t.SmileyId == id).ToList();

				if (thoughts.Any()) {
					DbContext.MessageThoughts.RemoveRange(thoughts);
				}

				// Only delete the file if no other smileys are using the file.
				if (!DbContext.Smileys.Any(s => s.FileName == smileyRecord.FileName)) {
					await ImageStore.Delete(new ImageStoreDeleteOptions {
						ContainerName = Constants.InternalKeys.SmileyImageContainer,
						Path = smileyRecord.Path
					});
				}

				await DbContext.SaveChangesAsync();

				TempData[Constants.InternalKeys.StatusMessage] = "The smiley was deleted.";
			}

			return this.RedirectToReferrer();
		}
	}
}