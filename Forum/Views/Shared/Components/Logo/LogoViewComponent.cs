using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum.Views.Shared.Components.Logo {
	public class LogoViewComponent : ViewComponent {
		public IViewComponentResult Invoke() {
			var holidayLogos = GetHolidays();

			var logoFile = "Logo.png";

			if (holidayLogos.ContainsKey(DateTime.Now.Date)) {
				logoFile = holidayLogos[DateTime.Now.Date];
			}
			else if (holidayLogos.ContainsKey(DateTime.Now.AddHours(-6).Date)) {
				logoFile = holidayLogos[DateTime.Now.AddHours(-6).Date];
			}

			var logoPath = $"/images/logos/{logoFile}";

			return View("Default", logoPath);
		}

		Dictionary<DateTime, string> GetHolidays() {
			var year = DateTime.Now.Year;

			var holidays = new Dictionary<DateTime, string>();

			// NEW YEARS 
			var newYearsDate = new DateTime(year, 1, 1).Date;

			// VALENTINES DAY
			var valentinesDay = new DateTime(year, 2, 14).Date;

			// ST PATRICKS DAY
			var stPatricksDay = new DateTime(year, 3, 17).Date;

			// ZOMBIE JESUS DAY
			var easter = year switch
			{
				2020 => new DateTime(year, 4, 12).Date,
				2021 => new DateTime(year, 4, 4).Date,
				2022 => new DateTime(year, 4, 17).Date,
				2023 => new DateTime(year, 4, 9).Date,
				2024 => new DateTime(year, 3, 31).Date,
				2025 => new DateTime(year, 4, 20).Date,
				2026 => new DateTime(year, 4, 5).Date,
				2027 => new DateTime(year, 3, 28).Date,
				2028 => new DateTime(year, 4, 16).Date,
				2029 => new DateTime(year, 4, 1).Date,
				2030 => new DateTime(year, 4, 21).Date,
				2031 => new DateTime(year, 4, 13).Date,
				2032 => new DateTime(year, 3, 28).Date,
				2033 => new DateTime(year, 4, 17).Date,
				2034 => new DateTime(year, 4, 9).Date,
				2035 => new DateTime(year, 3, 25).Date,
				2036 => new DateTime(year, 4, 13).Date,
				2037 => new DateTime(year, 4, 5).Date,
				2038 => new DateTime(year, 4, 25).Date,
				2039 => new DateTime(year, 4, 10).Date,
				_ => DateTime.Now.AddDays(-1)
			};

			// MEMORIAL DAY  -- last monday in May 
			var memorialDay = new DateTime(year, 5, 31);

			var dayOfWeek = memorialDay.DayOfWeek;

			while (dayOfWeek != DayOfWeek.Monday) {
				memorialDay = memorialDay.AddDays(-1);
				dayOfWeek = memorialDay.DayOfWeek;
			}

			// STAR WARS
			var starWarsDay = new DateTime(year, 5, 4).Date;

			// INDEPENCENCE DAY 
			var independenceDay = new DateTime(year, 7, 4).Date;

			// LABOR DAY -- 1st Monday in September
			var laborDay = new DateTime(year, 9, 1);

			dayOfWeek = laborDay.DayOfWeek;

			while (dayOfWeek != DayOfWeek.Monday) {
				laborDay = laborDay.AddDays(1);
				dayOfWeek = laborDay.DayOfWeek;
			}

			// TALK LIKE A PIRATE DAY
			var pirateDay = new DateTime(year, 9, 19).Date;

			// HALLOWEEN
			var halloween = new DateTime(year, 10, 31);

			// VETERANS DAY
			var veteransDay = new DateTime(year, 11, 11);

			// THANKSGIVING DAY - 4th Thursday in November 
			var thanksgiving = (from day in Enumerable.Range(1, 30)
								where new DateTime(year, 11, day).DayOfWeek == DayOfWeek.Thursday
								select day).ElementAt(3);

			var thanksgivingDay = new DateTime(year, 11, thanksgiving);
			var christmasEve = new DateTime(year, 12, 24).Date;
			var christmasDay = new DateTime(year, 12, 25).Date;

			// NEW YEARS EVE
			var newYearsEveDate = new DateTime(year, 12, 31).Date;

			holidays.Add(newYearsDate, "Logo_NewYears.png");
			holidays.Add(valentinesDay, "Logo_Valentines.png");
			holidays.Add(stPatricksDay, "Logo_StPatrick.png");
			//holidays.Add(memorialDay.Date, "Logo.png");
			holidays.Add(easter, "Logo_Easter.png");
			holidays.Add(starWarsDay, "Logo_StarWars.png");
			holidays.Add(independenceDay, "Logo_Independence.png");
			//holidays.Add(laborDay.Date, "Logo.png");
			holidays.Add(pirateDay, "Logo_Pirate.png");
			holidays.Add(halloween.Date, "Logo_Halloween.png");
			holidays.Add(veteransDay.Date, "Logo_VeteransDay.png");
			holidays.Add(thanksgivingDay.Date, "Logo_Thanksgiving.png");
			holidays.Add(christmasEve, "Logo_Christmas.png");
			holidays.Add(christmasDay, "Logo_Christmas.png");
			holidays.Add(newYearsEveDate, "Logo_NewYears.png");

			return holidays;
		}
	}
}
