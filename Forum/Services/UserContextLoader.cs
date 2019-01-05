using Forum.Contexts;
using Forum.Enums;
using Forum.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
					await LoadViewLogs(UserContext);					

					if (HttpContextAccessor.HttpContext.Request.Headers["X-Requested-With"] != "XMLHttpRequest") {
						await UpdateLastOnline();
					}
				}
			}
		}

		async Task LoadUserRoles(UserContext userContext) {
			var userRolesQuery = from userRole in await RoleRepository.UserRoles()
								 join role in await RoleRepository.SiteRoles() on userRole.RoleId equals role.Id
								 where userRole.UserId.Equals(userContext.ApplicationUser.Id)
								 select role.Id;

			var adminUsersQuery = from user in await AccountRepository.Records()
								  join userRole in await RoleRepository.UserRoles() on user.Id equals userRole.UserId
								  join role in await RoleRepository.SiteRoles() on userRole.RoleId equals role.Id
								  where role.Name == Constants.InternalKeys.Admin
								  select user.Id;

			userContext.Roles = userRolesQuery.ToList() ?? new List<string>();

			var adminRole = (await RoleRepository.SiteRoles()).FirstOrDefault(r => r.Name == Constants.InternalKeys.Admin);

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

		async Task LoadViewLogs(UserContext userContext) {
			var historyTimeLimit = DateTime.Now.AddDays(-14);

			// We shouldn't filter by time here because we're going to remove the expired ones below.
			var viewLogsQuery = from record in DbContext.ViewLogs
								where record.UserId == userContext.ApplicationUser.Id
								select record;

			var expiredViewLogsQuery = from record in viewLogsQuery
									   where record.LogTime <= historyTimeLimit
									   select record;

			DbContext.ViewLogs.RemoveRange(expiredViewLogsQuery);

			DbContext.ViewLogs.Add(new DataModels.ViewLog {
				LogTime = historyTimeLimit,
				TargetType = EViewLogTargetType.All,
				UserId = userContext.ApplicationUser.Id
			});

			await DbContext.SaveChangesAsync();

			userContext.ViewLogs = await viewLogsQuery.ToListAsync();
		}

		async Task UpdateLastOnline() {
			UserContext.ApplicationUser.LastOnline = DateTime.Now;
			DbContext.Update(UserContext.ApplicationUser);
			await DbContext.SaveChangesAsync();
			await ForumHub.Clients.All.SendAsync("whos-online");
		}
	}
}