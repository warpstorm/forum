using Forum3.Controllers;
using Forum3.Interfaces.Services;
using Forum3.Middleware;
using Forum3.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Services {
	using ServiceModels = Models.ServiceModels;

	public class ForumViewResult : IForumViewResult {
		BoardRepository BoardRepository { get; }
		IUrlHelper UrlHelper { get; }

		public ForumViewResult(
			BoardRepository boardRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			BoardRepository = boardRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public IActionResult RedirectToReferrer(Controller controller) {
			var referrer = GetReferrer(controller);
			return controller.Redirect(referrer);
		}

		public async Task<IActionResult> RedirectFromService(Controller controller, ServiceModels.ServiceResponse serviceResponse, Func<Task<IActionResult>> failureCallback) {
			if (!string.IsNullOrEmpty(serviceResponse.Message))
				controller.TempData[Constants.Keys.StatusMessage] = serviceResponse.Message;

			foreach (var kvp in serviceResponse.Errors)
				controller.ModelState.AddModelError(kvp.Key, kvp.Value);

			if (serviceResponse.Success) {
				var redirectPath = serviceResponse.RedirectPath;

				if (string.IsNullOrEmpty(redirectPath))
					redirectPath = GetReferrer(controller);

				return controller.Redirect(redirectPath);
			}

			return await failureCallback();
		}

		public IActionResult RedirectToLocal(Controller controller, string returnUrl) {
			if (controller.Url.IsLocalUrl(returnUrl))
				return controller.Redirect(returnUrl);
			else
				return controller.RedirectToAction(nameof(Home.FrontPage), nameof(Home));
		}

		public IActionResult ViewResult(Controller controller, string viewName, object model = null) {
			controller.ViewData["LogoPath"] = GetLogoPath();
			controller.ViewData["Referrer"] = GetReferrer(controller);
			controller.ViewData["Categories"] = BoardRepository.CategoryIndex();

			if (controller.HttpContext.Items["PageTimer"] is Stopwatch pageTimer) {
				pageTimer.Stop();
				var pageTimerSeconds = 1D * pageTimer.ElapsedMilliseconds / 1000;

				controller.ViewData["FooterPageTimer"] = $" | {pageTimerSeconds} seconds";
			}

			return controller.View(viewName, model);
		}
		public IActionResult ViewResult(Controller controller, object model) => ViewResult(controller, null, model);
		public IActionResult ViewResult(Controller controller) => ViewResult(controller, null, null);

		string GetReferrer(Controller controller) {
			controller.Request.Query.TryGetValue("ReturnUrl", out var referrer);

			if (string.IsNullOrEmpty(referrer))
				controller.Request.Query.TryGetValue("Referer", out referrer);

			if (string.IsNullOrEmpty(referrer))
				referrer = UrlHelper.Action(nameof(Home.FrontPage), nameof(Home));

			return referrer;
		}

		string GetLogoPath() {
			var holidayLogos = GetHolidays();

			var logoFile = "Logo.png";

			if (holidayLogos.ContainsKey(DateTime.Now.Date))
				logoFile = holidayLogos[DateTime.Now.Date];

			return $"/images/logos/{logoFile}";
		}

		Dictionary<DateTime, string> GetHolidays() {
			var year = DateTime.Now.Year;

			var holidays = new Dictionary<DateTime, string>();

			//NEW YEARS 
			var newYearsDate = new DateTime(year, 1, 1).Date;

			//VALENTINES DAY
			var valentinesDay = new DateTime(year, 2, 14).Date;

			//MEMORIAL DAY  -- last monday in May 
			var memorialDay = new DateTime(year, 5, 31);

			var dayOfWeek = memorialDay.DayOfWeek;

			while (dayOfWeek != DayOfWeek.Monday) {
				memorialDay = memorialDay.AddDays(-1);
				dayOfWeek = memorialDay.DayOfWeek;
			}

			// ST PATRICKS DAY
			var stPatricksDay = new DateTime(year, 3, 17).Date;

			// STAR WARS
			var starWarsDay = new DateTime(year, 5, 4).Date;

			//INDEPENCENCE DAY 
			var independenceDay = new DateTime(year, 7, 4).Date;

			//LABOR DAY -- 1st Monday in September
			var laborDay = new DateTime(year, 9, 1);

			dayOfWeek = laborDay.DayOfWeek;

			while (dayOfWeek != DayOfWeek.Monday) {
				laborDay = laborDay.AddDays(1);
				dayOfWeek = laborDay.DayOfWeek;
			}

			// TALK LIKE A PIRATE DAY
			var pirateDay = new DateTime(year, 9, 19).Date;

			//THANKSGIVING DAY - 4th Thursday in November 
			var thanksgiving = (from day in Enumerable.Range(1, 30)
								where new DateTime(year, 11, day).DayOfWeek == DayOfWeek.Thursday
								select day).ElementAt(3);

			var thanksgivingDay = new DateTime(year, 11, thanksgiving);
			var christmasEve = new DateTime(year, 12, 24).Date;
			var christmasDay = new DateTime(year, 12, 25).Date;

			holidays.Add(newYearsDate, "Logo_NewYears.png");
			holidays.Add(valentinesDay, "Logo_Valentines.png");
			//holidays.Add(memorialDay.Date, "Logo.png");
			holidays.Add(stPatricksDay, "Logo_StPatrick.png");
			holidays.Add(starWarsDay, "Logo_StarWars.png");
			holidays.Add(independenceDay, "Logo_Independence.png");
			//holidays.Add(laborDay.Date, "Logo.png");
			holidays.Add(pirateDay, "Logo_Pirate.png");
			holidays.Add(thanksgivingDay.Date, "Logo_Thanksgiving.png");
			holidays.Add(christmasEve, "Logo_Christmas.png");
			holidays.Add(christmasDay, "Logo_Christmas.png");

			return holidays;
		}
	}
}