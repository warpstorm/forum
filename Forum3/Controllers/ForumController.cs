using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Forum3.Models.ServiceModels;
using Forum3.Services;

namespace Forum3.Controllers {
	public class ForumController : Controller {
		UserService UserService { get; set; }

		public ForumController(UserService userService) {
			UserService = userService;
		}

		protected void ProcessServiceResponse(ServiceResponse serviceResponse) {
			if (!string.IsNullOrEmpty(serviceResponse.Message))
				TempData[Names.Keys.StatusMessage] = serviceResponse.Message;

			foreach (var kvp in serviceResponse.ModelErrors)
				ModelState.AddModelError(kvp.Key, kvp.Value);
		}

		public override ViewResult View(object model) => View(null, model);
		public override ViewResult View(string viewName, object model = null) {
			UniversalViewActions();
			return base.View(viewName, model);
		}

		void UniversalViewActions() {
			//if (UserService.ContextUser.IsAuthenticated)
			//	SetLastOnline();

			//ViewBag.Notifications = Notifications.Create(Db, WebSecurity.CurrentUserId);
			//ViewBag.SmileyPath = SiteSettings.Get(SiteSettingNames.SmileyPath, Db);
			//ViewBag.ForumVersion = "Theme "
			//						+ WebConfigurationManager.AppSettings.Get("Forum:Theme")
			//						+ " | Code "
			//						+ WebConfigurationManager.AppSettings.Get("Forum:VersionMajor")
			//						+ "."
			//						+ WebConfigurationManager.AppSettings.Get("Forum:VersionFeatureSet")
			//						+ "."
			//						+ WebConfigurationManager.AppSettings.Get("Forum:VersionFix");

			ViewData["LogoPath"] = "/images/logos/" + GetLogoPath();
		}


		string GetLogoPath() {
			var holidayLogos = GetHolidays();

			if (holidayLogos.ContainsKey(DateTime.Now.Date))
				return holidayLogos[DateTime.Now.Date];

			return "Logo.png";
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

			DateTime thanksgivingDay = new DateTime(year, 11, thanksgiving);
			DateTime christmasEve = new DateTime(year, 12, 24).Date;
			DateTime christmasDay = new DateTime(year, 12, 25).Date;

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