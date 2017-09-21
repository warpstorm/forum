using Forum3.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forum3.Controllers {
	public class MigratorController : Controller {
		MigratorService MigratorService { get; }

		public MigratorController(
			MigratorService migratorService
		) {
			MigratorService = migratorService;
		}

		public IActionResult Test() {
			ViewData["result"] = MigratorService.Test();

			return View();
		}
	}
}