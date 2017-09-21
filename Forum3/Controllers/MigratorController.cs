using System;
using System.Threading.Tasks;
using Forum3.Helpers;
using Forum3.Services;
using Microsoft.AspNetCore.Authorization;
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

			return View("Done");
		}

		[AllowAnonymous]
		public async Task<IActionResult> Users() {
			ViewData["result"] = await MigratorService.MigrateUsers();

			return View("Done");
		}
	}
}