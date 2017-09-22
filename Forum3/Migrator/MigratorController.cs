using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Migrator {
	public class MigratorController : Controller {
		MigratorService MigratorService { get; }

		public MigratorController(
			MigratorService migratorService
		) {
			MigratorService = migratorService;
		}

		public async Task<IActionResult> ConnectionTest() {
			ViewData["result"] = await MigratorService.ConnectionTest();
			return View("Done");
		}

		[AllowAnonymous]
		public async Task<IActionResult> Run() {
			ViewData["result"] = await MigratorService.Execute();
			return View("Done");
		}
	}
}