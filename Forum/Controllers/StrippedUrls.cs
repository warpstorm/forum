using Forum.Controllers.Annotations;
using Forum.Extensions;
using Forum.Models.DataModels;
using Forum.Models.ServiceModels;
using Forum.Services;
using Forum.Services.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels.StrippedUrls;

	[Authorize(Roles = Constants.InternalKeys.Admin)]
	public class StrippedUrls : Controller {
		ApplicationDbContext DbContext { get; }

		public StrippedUrls(ApplicationDbContext dbContext) => DbContext = dbContext;

		[ActionLog]
		[HttpGet]
		public IActionResult Index() {
			var records = DbContext.StrippedUrls.ToList();

			var items = new List<ViewModels.IndexItem>();

			foreach (var record in records) {
				items.Add(new ViewModels.IndexItem {
					Url = record.Url,
					RegexPattern = record.RegexPattern
				});
			}

			var viewModel = new ViewModels.IndexPage {
				StrippedUrls = items
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Index(InputModels.EditStrippedUrlsInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = new ServiceResponse();

				var records = DbContext.StrippedUrls.ToList();
				var inputs = input.StrippedUrls?.ToList();

				foreach (var record in records) {
					InputModels.EditStrippedUrlInput inputItem = null;
					var inputIndex = -1;

					for (var i = 0; i < inputs.Count; i++) {
						if (inputs[i].Url == record.Url) {
							inputItem = inputs[i];
							inputIndex = i;
						}
					}

					if (inputItem is null) {
						DbContext.Remove(record);
					}
					else {
						ValidateRegex($"{nameof(ViewModels.IndexPage.StrippedUrls)}[{inputIndex}].{nameof(ViewModels.IndexItem.RegexPattern)}", inputItem.RegexPattern, serviceResponse);

						record.RegexPattern = inputItem.RegexPattern;
						inputs?.Remove(inputItem);
					}
				}

				if (!string.IsNullOrEmpty(input.NewUrl)) {
					ValidateRegex(nameof(ViewModels.IndexPage.NewRegex), input.NewRegex, serviceResponse);

					if (serviceResponse.Success) {
						DbContext.Add(new StrippedUrl {
							Url = input.NewUrl,
							RegexPattern = input.NewRegex
						});
					}
				}

				if (serviceResponse.Success) {
					DbContext.SaveChanges();

					return await this.RedirectFromService(serviceResponse);
				}

				foreach (var kvp in serviceResponse.Errors) {
					ModelState.AddModelError(kvp.Key, kvp.Value);
				}
			}

			var strippedUrls = new List<ViewModels.IndexItem>();

			foreach (var item in input.StrippedUrls) {
				strippedUrls.Add(new ViewModels.IndexItem {
					Url = item.Url,
					RegexPattern = item.RegexPattern
				});
			}

			var viewModel = new ViewModels.IndexPage {
				NewUrl = input.NewUrl,
				NewRegex = input.NewRegex,
				StrippedUrls = strippedUrls
			};

			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Delete(string url) {
			var decodedUrl = Uri.UnescapeDataString(url);

			var record = DbContext.StrippedUrls.Find(decodedUrl);

			if (!(record is null)) {
				DbContext.Remove(record);
				DbContext.SaveChanges();
			}

			return this.RedirectToReferrer();
		}

		void ValidateRegex(string key, string pattern, ServiceResponse serviceResponse) {
			if (string.IsNullOrEmpty(pattern)) {
				serviceResponse.Error(key, "Regex pattern cannot be empty when a new URL is defined.");
			}

			try {
				Regex.Match(string.Empty, pattern);
			}
			catch (ArgumentException) {
				serviceResponse.Error(key, "Regex pattern is invalid.");
			}

			if (!Regex.Match(pattern, @"^[^\(]*\({1}[^\(]*\){1}[^\(]*$").Success) {
				serviceResponse.Error(key, "Regex pattern must contain exactly one group.");
			}
		}
	}
}
