using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Forum3.Data;
using Forum3.Models.DataModels;
using Forum3.Helpers;
using Forum3.Models.ServiceModels;
using Forum3.Models.ViewModels.Boards.Items;

namespace Forum3.Services {
	public class UserService {
		// NOTE - This Result is a blocking call, required because this property can't be async. I should find a better solution like a UserServiceFactory or something.
		public ContextUser ContextUser => _ContextUser ?? (_ContextUser = GetContextUser().Result);
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

		public async Task<List<OnlineUser>> GetOnlineUsers() {
			await GetContextUser();

			var onlineTimeLimit = DateTime.Now.AddMinutes(SiteSettingsService.GetInt(Constants.SiteSettings.OnlineTimeLimit));
			var onlineTodayTimeLimit = DateTime.Now.AddMinutes(-10080);

			var onlineUsers = await (from user in DbContext.Users
				where user.LastOnline >= onlineTodayTimeLimit
				orderby user.LastOnline descending
				select new OnlineUser {
					Id = user.Id,
					Name = user.DisplayName,
					Online = user.LastOnline >= onlineTimeLimit,
					LastOnline = user.LastOnline
				}).ToListAsync();

			foreach (var onlineUser in onlineUsers)
				onlineUser.LastOnlineString = onlineUser.LastOnline.ToPassedTimeString();

			return onlineUsers;
		}

		public async Task<List<string>> GetBirthdays() {
			var todayBirthdayNames = new List<string>();

			var birthdays = await DbContext.Users.Select(u => new Birthday {
				Date = u.Birthday,
				DisplayName = u.DisplayName
			}).ToListAsync();

			if (birthdays.Any()) {
				var todayBirthdays = birthdays.Where(u => new DateTime(DateTime.Now.Year, u.Date.Month, u.Date.Day).Date == DateTime.Now.Date);

				foreach (var item in todayBirthdays) {
					var now = DateTime.Today;
					var age = now.Year - item.Date.Year;

					if (item.Date > now.AddYears(-age)) 
						age--;

					todayBirthdayNames.Add($"{item.DisplayName} ({age})");
				}
			}

			return todayBirthdayNames;
		}

		/// <summary>
		/// Ensures the ContextUser is loaded.
		/// </summary>
		async Task<ContextUser> GetContextUser() {
			if (_ContextUser != null)
				return _ContextUser;

			var contextUser = new ContextUser();
			var currentPrincipal = HttpContextAccessor.HttpContext.User;

			if (currentPrincipal.Identity.IsAuthenticated) {
				contextUser.IsAuthenticated = true;
				contextUser.IsAdmin = currentPrincipal.IsInRole("Admin");
				contextUser.IsVetted = currentPrincipal.IsInRole("Vetted");

				contextUser.ApplicationUser = await UserManager.GetUserAsync(currentPrincipal);
				contextUser.ApplicationUser.LastOnline = DateTime.Now;
				DbContext.Entry(contextUser.ApplicationUser).State = EntityState.Modified;
				await DbContext.SaveChangesAsync();
			}

			return _ContextUser = contextUser;
		}

		class Birthday {
			public string DisplayName { get; set; }
			public DateTime Date { get; set; }
		}
	}
}
