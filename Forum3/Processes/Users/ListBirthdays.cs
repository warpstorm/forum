using Forum3.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Processes.Users {
	public class ListBirthdays {
		ApplicationDbContext DbContext { get; }

		public ListBirthdays(
			ApplicationDbContext dbContext
		) {
			DbContext = dbContext;
		}

		public List<string> Execute() {
			var todayBirthdayNames = new List<string>();

			var birthdays = DbContext.Users.Select(u => new {
				u.Birthday,
				u.DisplayName
			}).ToList();

			if (birthdays.Any()) {
				var todayBirthdays = birthdays.Where(u => new DateTime(DateTime.Now.Year, u.Birthday.Month, u.Birthday.Day).Date == DateTime.Now.Date);

				foreach (var item in todayBirthdays) {
					var now = DateTime.Today;
					var age = now.Year - item.Birthday.Year;

					if (item.Birthday > now.AddYears(-age))
						age--;

					todayBirthdayNames.Add($"{item.DisplayName} ({age})");
				}
			}

			return todayBirthdayNames;
		}

	}
}