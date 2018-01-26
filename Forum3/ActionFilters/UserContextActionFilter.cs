using Forum3.Enums;
using Forum3.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.ActionFilters {
	using DataModels = Models.DataModels;
	using ServiceModels = Models.ServiceModels;

	public class UserContextActionFilter : IAsyncActionFilter {
		DataModels.ApplicationDbContext DbContext { get; }
		ServiceModels.UserContext UserContext { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		SettingsRepository Settings { get; }

		public UserContextActionFilter(
			DataModels.ApplicationDbContext dbContext,
			ServiceModels.UserContext userContext,
			UserManager<DataModels.ApplicationUser> userManager,
			SignInManager<DataModels.ApplicationUser> signInManager,
			SettingsRepository settingsRepository
		) {
			DbContext = dbContext;
			UserContext = userContext;
			UserManager = userManager;
			SignInManager = signInManager;
			Settings = settingsRepository;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var currentPrincipal = context.HttpContext.User;

			if (currentPrincipal.Identity.IsAuthenticated) {
				UserContext.ApplicationUser = await UserManager.GetUserAsync(currentPrincipal);

				if (UserContext.ApplicationUser is null) {
					await SignInManager.SignOutAsync();
				}
				else {
					await LoadUserRoles();
					await LoadViewLogs();
					await UpdateLastOnline();
				}
			}

			await next();
		}

		public async Task LoadUserRoles() {
			var userRolesQuery = from userRole in DbContext.UserRoles
								 join role in DbContext.Roles on userRole.RoleId equals role.Id
								 where userRole.UserId.Equals(UserContext.ApplicationUser.Id)
								 select role.Name;

			var adminUsersQuery = from user in DbContext.Users
								  join userRole in DbContext.UserRoles on user.Id equals userRole.UserId
								  join role in DbContext.Roles on userRole.RoleId equals role.Id
								  where role.Name == "Admin"
								  select user.Id;

			var userRolesQueryTask = userRolesQuery.ToListAsync();
			var adminRoleTask = DbContext.Roles.SingleOrDefaultAsync(r => r.Name == "Admin");
			var anyAdminUsersTask = adminUsersQuery.AnyAsync();

			await Task.WhenAll(new Task[] {
				userRolesQueryTask,
				adminRoleTask,
				anyAdminUsersTask
			});

			UserContext.Roles = userRolesQueryTask.Result;
			var adminRole = adminRoleTask.Result;
			var anyAdminUsers = anyAdminUsersTask.Result;

			// Occurs when there is no admin role created yet.
			if (adminRole is null)
				UserContext.IsAdmin = true;

			// Occurs when there is an admin role, but no admin users yet.
			if (!anyAdminUsers)
				UserContext.IsAdmin = true;

			if (UserContext.Roles.Contains("Admin")) {
				// Force logout if the user was removed from Admin, but their session still says they're in Admin.
				if (!adminUsersQuery.Any(uid => uid == UserContext.ApplicationUser.Id)) {
					await SignInManager.SignOutAsync();
					return;
				}

				UserContext.IsAdmin = true;
			}

			UserContext.IsAuthenticated = true;
		}

		public async Task UpdateLastOnline() {
			UserContext.ApplicationUser.LastOnline = DateTime.Now;
			DbContext.Update(UserContext.ApplicationUser);
			await DbContext.SaveChangesAsync();
		}

		public async Task LoadViewLogs() {
			var historyTimeLimit = Settings.HistoryTimeLimit().AddDays(-1);

			var viewLogsQuery = from record in DbContext.ViewLogs
								where record.UserId == UserContext.ApplicationUser.Id
								orderby record.LogTime descending
								select record;

			var viewLogs = await viewLogsQuery.ToListAsync();

			var expiredViewLogsQuery = from record in viewLogs
									   where record.TargetType == EViewLogTargetType.All
									   select record;

			var expiredViewLogs = expiredViewLogsQuery.ToList();

			if (expiredViewLogs.Where(record => record.LogTime <= historyTimeLimit).Any()) {
				foreach (var viewLog in expiredViewLogs)
					DbContext.ViewLogs.Remove(viewLog);

				// Gives them a day before the next update so we don't do this every request.
				historyTimeLimit = historyTimeLimit.AddDays(1);

				DbContext.ViewLogs.Add(new DataModels.ViewLog {
					LogTime = historyTimeLimit,
					TargetType = EViewLogTargetType.All,
					UserId = UserContext.ApplicationUser.Id
				});
			}

			UserContext.ViewLogs = await viewLogsQuery.ToListAsync();
		}
	}
}