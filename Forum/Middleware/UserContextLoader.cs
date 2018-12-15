using Forum.Contexts;
using Forum.Enums;
using Forum.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Middleware {
	using DataModels = Models.DataModels;

	public class UserContextLoader {
		RequestDelegate Next { get; }

		#region Dependencies which cannot be loaded from the root scope, and must therefore be injected during Invoke()
		ApplicationDbContext DbContext { get; set; }
		UserContext UserContext { get; set; }
		SettingsRepository SettingsRepository { get; set; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; set; }
		UserManager<DataModels.ApplicationUser> UserManager { get; set; }
		#endregion

		public UserContextLoader(RequestDelegate next) {
			Next = next;
		}

		public async Task Invoke(
			HttpContext context,
			ApplicationDbContext dbContext,
			UserContext userContext,
			SettingsRepository settingsRepository,
			SignInManager<DataModels.ApplicationUser> signInManager,
			UserManager<DataModels.ApplicationUser> userManager
		) {
			DbContext = dbContext;
			UserContext = userContext;
			SettingsRepository = settingsRepository;
			SignInManager = signInManager;
			UserManager = userManager;

			var currentPrincipal = context.User;

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

			await Next(context);
		}

		async Task LoadUserRoles() {
			var userRolesQuery = from userRole in DbContext.UserRoles
								 join role in DbContext.Roles on userRole.RoleId equals role.Id
								 where userRole.UserId.Equals(UserContext.ApplicationUser.Id)
								 select role.Id;

			var adminUsersQuery = from user in DbContext.Users
								  join userRole in DbContext.UserRoles on user.Id equals userRole.UserId
								  join role in DbContext.Roles on userRole.RoleId equals role.Id
								  where role.Name == "Admin"
								  select user.Id;

			UserContext.Roles = userRolesQuery.ToList();
			var adminRole = DbContext.Roles.FirstOrDefault(r => r.Name == "Admin");
			var anyAdminUsers = adminUsersQuery.Any();

			if (adminRole != null && UserContext.Roles.Contains(adminRole.Id)) {
				// Force logout if the user was removed from Admin, but their session still says they're in Admin.
				if (!adminUsersQuery.Any(uid => uid == UserContext.ApplicationUser.Id)) {
					await SignInManager.SignOutAsync();
					return;
				}

				UserContext.IsAdmin = true;
			}

			UserContext.IsAuthenticated = true;
		}

		async Task UpdateLastOnline() {
			UserContext.ApplicationUser.LastOnline = DateTime.Now;
			DbContext.Update(UserContext.ApplicationUser);
			await DbContext.SaveChangesAsync();
		}

		async Task LoadViewLogs() {
			var historyTimeLimit = SettingsRepository.HistoryTimeLimit().AddDays(-1);

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
				foreach (var viewLog in expiredViewLogs) {
					DbContext.ViewLogs.Remove(viewLog);
				}

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