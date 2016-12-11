using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Forum3.Data;
using Forum3.DataModels;
using Forum3.Helpers;
using Forum3.ViewModels.Shared;
using Forum3.ServiceModels;

namespace Forum3.Services {
	public class UserService {
		public ContextUser ContextUser {
			get {
				if (_ContextUser == null)
					LoadContextUser();

				return _ContextUser;
			}
			private set {
				_ContextUser = value;
			}
		}
		ContextUser _ContextUser;

		ApplicationDbContext DbContext { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		UserManager<ApplicationUser> UserManager { get; }
		SiteSettingsService SiteSettingsService { get; }

		public UserService(
			ApplicationDbContext dbContext,
			IHttpContextAccessor httpContextAccessor,
			UserManager<ApplicationUser> userManager,
			SiteSettingsService siteSettingsService
		) {
			DbContext = dbContext;
			HttpContextAccessor = httpContextAccessor;
			UserManager = userManager;
			SiteSettingsService = siteSettingsService;
		}

		public List<OnlineUser> GetOnlineUsers() {
			var onlineTimeLimit = DateTime.Now.AddMinutes(SiteSettingsService.GetInt(Names.SiteSettings.OnlineTimeLimit));
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsers = DbContext.Users.Where(u => u.LastOnline >= onlineTodayTimeLimit).OrderByDescending(u => u.LastOnline).Select(u => new OnlineUser {
				Id = u.Id,
				Name = u.DisplayName,
				Online = u.LastOnline >= onlineTimeLimit,
				LastOnline = u.LastOnline
			}).ToList();

			foreach (var onlineUser in onlineUsers)
				onlineUser.LastOnlineString = onlineUser.LastOnline.ToPassedTimeString();

			return onlineUsers;
		}

		public List<string> GetBirthdays() {
			var todayBirthdayNames = new List<string>();

			var birthdays = DbContext.Users.Select(u => new Birthday {
				Date = u.Birthday,
				DisplayName = u.DisplayName
			}).ToList();

			if (birthdays.Count > 0) {
				var todayBirthdays = birthdays.Where(u => new DateTime(DateTime.Now.Year, u.Date.Month, u.Date.Day).Date == DateTime.Now.Date);

				foreach (var item in todayBirthdays) {
					DateTime now = DateTime.Today;
					int age = now.Year - item.Date.Year;
					if (item.Date > now.AddYears(-age)) age--;

					todayBirthdayNames.Add(item.DisplayName + " (" + age + ")");
				}
			}

			return todayBirthdayNames;
		}

		void LoadContextUser() {
			var contextUser = new ContextUser();

			var currentPrincipal = HttpContextAccessor.HttpContext.User;

			// TODO - This is a blocking call. Find a better solution like a UserServiceFactory or something.
			var currentUser = UserManager.GetUserAsync(currentPrincipal).Result;

			contextUser.Id = currentUser.Id;

			if (currentPrincipal.Identity.IsAuthenticated) {
				contextUser.IsAuthenticated = true;
				contextUser.IsAdmin = currentPrincipal.IsInRole("Admin");
				contextUser.IsVetted = currentPrincipal.IsInRole("Vetted");
			}

			ContextUser = contextUser;
		}

		class Birthday {
			public string DisplayName { get; set; }
			public DateTime Date { get; set; }
		}
	}
}
