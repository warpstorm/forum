using Forum.Contexts;
using Forum.Enums;
using Forum.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Services {
	using DataModels = Models.DataModels;

	public class UserContextLoader {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		RoleRepository RoleRepository { get; }
		AccountRepository AccountRepository { get; }
		IHubContext<ForumHub> ForumHub { get; }
		SignInManager<DataModels.ApplicationUser> SignInManager { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }
		IHttpContextAccessor HttpContextAccessor { get; }

		public UserContextLoader(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			RoleRepository roleRepository,
			IHubContext<ForumHub> forumHub,
			SignInManager<DataModels.ApplicationUser> signInManager,
			UserManager<DataModels.ApplicationUser> userManager,
			IHttpContextAccessor httpContextAccessor
		) {
			DbContext = dbContext;
			UserContext = userContext;
			RoleRepository = roleRepository;
			AccountRepository = accountRepository;
			ForumHub = forumHub;
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

					if (HttpContextAccessor.HttpContext.Request.Headers["X-Requested-With"] != "XMLHttpRequest") {
						await UpdateLastOnline();
					}
				}
			}
		}

		async Task LoadUserRoles(UserContext userContext) {
			var userRolesQuery = from userRole in RoleRepository.UserRoles
								 join role in RoleRepository.SiteRoles on userRole.RoleId equals role.Id
								 where userRole.UserId.Equals(userContext.ApplicationUser.Id)
								 select role.Id;

			var adminUsersQuery = from user in AccountRepository
								  join userRole in RoleRepository.UserRoles on user.Id equals userRole.UserId
								  join role in RoleRepository.SiteRoles on userRole.RoleId equals role.Id
								  where role.Name == Constants.InternalKeys.Admin
								  select user.Id;

			userContext.Roles = userRolesQuery.ToList() ?? new List<string>();

			var adminRole = RoleRepository.SiteRoles.FirstOrDefault(r => r.Name == Constants.InternalKeys.Admin);
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

		void LoadViewLogs(UserContext userContext) {
			var historyTimeLimit = DateTime.Now.AddDays(-14);

			// We shouldn't filter by time here because we're going to remove the expired ones below.
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

				DbContext.SaveChanges();
			}

			userContext.ViewLogs = viewLogsQuery.ToList();
		}

		async Task UpdateLastOnline() {
			UserContext.ApplicationUser.LastOnline = DateTime.Now;
			DbContext.Update(UserContext.ApplicationUser);
			DbContext.SaveChanges();
			await ForumHub.Clients.All.SendAsync("whos-online");
		}
	}
}