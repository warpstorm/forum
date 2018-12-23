using Forum.Contexts;
using Forum.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services {
	using DataModels = Models.DataModels;

	public class UserContextLoader {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IHttpContextAccessor HttpContextAccessor { get; }

		public UserContextLoader(
			ApplicationDbContext dbContext,
			UserContext userContext,
			SignInManager<DataModels.ApplicationUser> signInManager,
			UserManager<DataModels.ApplicationUser> userManager,
			IHttpContextAccessor httpContextAccessor
		) {
			DbContext = dbContext;
			UserContext = userContext;
			SignInManager = signInManager;
			UserManager = userManager;
			HttpContextAccessor = httpContextAccessor;
		}

		public async Task Invoke() {
			if (HttpContextAccessor.HttpContext.User.Identity.IsAuthenticated) {
				UserContext.ApplicationUser = await UserManager.GetUserAsync(HttpContextAccessor.HttpContext.User);

				if (UserContext.ApplicationUser is null) {
					await SignInManager.SignOutAsync();
				}
				else {
					await LoadUserRoles(UserContext);
					LoadViewLogs(UserContext);
					await UpdateLastOnline(UserContext);
				}
			}
		}

		async Task LoadUserRoles(UserContext userContext) {
			var userRolesQuery = from userRole in DbContext.UserRoles
								 join role in DbContext.Roles on userRole.RoleId equals role.Id
								 where userRole.UserId.Equals(userContext.ApplicationUser.Id)
								 select role.Id;

			var adminUsersQuery = from user in DbContext.Users
								  join userRole in DbContext.UserRoles on user.Id equals userRole.UserId
								  join role in DbContext.Roles on userRole.RoleId equals role.Id
								  where role.Name == Constants.InternalKeys.Admin
								  select user.Id;

			userContext.Roles = userRolesQuery.ToList() ?? new List<string>();

			var adminRole = DbContext.Roles.FirstOrDefault(r => r.Name == Constants.InternalKeys.Admin);
			var anyAdminUsers = adminUsersQuery.Any();

			if (adminRole != null && userContext.Roles.Contains(adminRole.Id)) {
				// Force logout if the user was removed from Admin, but their session still says they're in Admin.
				if (!adminUsersQuery.Any(uid => uid == userContext.ApplicationUser.Id)) {
					await SignInManager.SignOutAsync();
					return;
				}

				userContext.IsAdmin = true;
			}

			userContext.IsAuthenticated = true;
		}

		async Task UpdateLastOnline(UserContext userContext) {
			userContext.ApplicationUser.LastOnline = DateTime.Now;
			DbContext.Update(userContext.ApplicationUser);
			await DbContext.SaveChangesAsync();
		}

		void LoadViewLogs(UserContext userContext) {
			var historyTimeLimit = GetHistoryTimeLimit(userContext);

			var viewLogsQuery = from record in DbContext.ViewLogs
								where record.UserId == userContext.ApplicationUser.Id
								orderby record.LogTime descending
								select record;

			var viewLogs = viewLogsQuery.ToList();

			var expiredViewLogsQuery = from record in viewLogs
									   where record.TargetType == EViewLogTargetType.All
									   select record;

			var expiredViewLogs = expiredViewLogsQuery.ToList();

			if (expiredViewLogs.Where(record => record.LogTime <= historyTimeLimit).Any()) {
				foreach (var viewLog in expiredViewLogs) {
					DbContext.ViewLogs.Remove(viewLog);
				}

				// Gives them a day before the next update so we don't do this every request.
				historyTimeLimit = historyTimeLimit.AddDays(1);

				DbContext.ViewLogs.Add(new DataModels.ViewLog {
					LogTime = historyTimeLimit,
					TargetType = EViewLogTargetType.All,
					UserId = userContext.ApplicationUser.Id
				});
			}

			userContext.ViewLogs = viewLogsQuery.ToList();
		}

		DateTime GetHistoryTimeLimit(UserContext userContext) {
			var historySettingValue = -14;

			var userSetting = DbContext.SiteSettings.FirstOrDefault(r => r.Name == Constants.Settings.HistoryTimeLimit && r.UserId == userContext.ApplicationUser.Id)?.Value;
			var globalSetting = DbContext.SiteSettings.FirstOrDefault(r => r.Name == Constants.Settings.HistoryTimeLimit && string.IsNullOrEmpty(r.UserId))?.Value;

			if (!string.IsNullOrEmpty(userSetting)) {
				historySettingValue = Convert.ToInt32(userSetting);
			}
			else if (!string.IsNullOrEmpty(globalSetting)) {
				historySettingValue = Convert.ToInt32(globalSetting);
			}

			var historyTimeLimit = DateTime.Now.AddDays(historySettingValue);
			return historyTimeLimit;
		}
	}
}