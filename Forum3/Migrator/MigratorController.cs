using System.Threading.Tasks;
using Forum3.Services;
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

		public IActionResult Test() {
			ViewData["result"] = MigratorService.Test();

			return View("Done");
		}

		[AllowAnonymous]
		public async Task<IActionResult> Run() {
			ViewData["result"] = await MigratorService.Execute();

			return View("Done");
		}
	}
}