using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Forum3.Migrator {
	using InputModels = Forum3.Models.InputModels;

	public class Migrator : Controller {
		MigratorService MigratorService { get; }

		public Migrator(
			MigratorService migratorService
		) {
			MigratorService = migratorService;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Run(InputModels.Continue input = null) {
			var viewModel = await MigratorService.Execute(input);
			return View("Delay", viewModel);
		}
	}
}