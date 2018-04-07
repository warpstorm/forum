using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Models.InputModels;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using ViewModels = Models.ViewModels.Smileys;

	[Authorize(Roles="Admin")]
	public class Smileys : ForumController {
		ApplicationDbContext DbContext { get; }
		SmileyRepository SmileyRepository { get; }

		public Smileys(
			ApplicationDbContext dbContext,
			SmileyRepository smileyRepository
		) {
			DbContext = dbContext;
			SmileyRepository = smileyRepository;
		}

		[HttpGet]
		public IActionResult Index() {
			var smileysQuery = from smiley in DbContext.Smileys
							   orderby smiley.SortOrder
							   select smiley;

			var smileys = smileysQuery.ToList();

			var viewModel = new ViewModels.IndexPage();

			foreach (var smiley in smileys) {
				var sortColumn = smiley.SortOrder / 1000;
				var sortRow = smiley.SortOrder % 1000;

				viewModel.Items.Add(new ViewModels.IndexItem {
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

		[HttpGet]
		public IActionResult Create() {
			var viewModel = new ViewModels.CreatePage();
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(CreateSmileyInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await SmileyRepository.Create(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
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
		public IActionResult Edit(EditSmileysInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = SmileyRepository.Update(input);
				ProcessServiceResponse(serviceResponse);
			}

			return RedirectFromService();
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			var serviceResponse = await SmileyRepository.Delete(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}
	}
}