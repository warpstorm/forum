using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using InputModels = Forum3.Models.InputModels;

namespace Forum3.Migrator {
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