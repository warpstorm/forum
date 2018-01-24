using Forum3.Helpers;
using Forum3.Models.DataModels;
using Forum3.Models.ServiceModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace Forum3.Services {
	public class ContextUserFactory {
		ApplicationDbContext DbContext { get; }
		UserManager<ApplicationUser> UserManager { get; }
		SignInManager<ApplicationUser> SignInManager { get; }
		HttpContext HttpContext { get; }

		public ContextUserFactory(
			ApplicationDbContext dbContext,
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IHttpContextAccessor httpContextAccessor
		) {
			DbContext = dbContext;
			UserManager = userManager;
			SignInManager = signInManager;

			HttpContext = httpContextAccessor.HttpContext;
		}

		public ContextUser GetContextUser() {
			HttpContext.Items.ThrowIfNull(nameof(HttpContext.Items));

			object value;

			if (HttpContext.Items.TryGetValue(typeof(ContextUser), out value) && value is ContextUser)
				return (ContextUser)value;

			var contextUser = ConstructContextUser();
			HttpContext.Items[typeof(ContextUser)] = contextUser;

			return contextUser;
		}

		ContextUser ConstructContextUser() {
			var currentPrincipal = HttpContext.User;

			var contextUser = new ContextUser();

			if (currentPrincipal.Identity.IsAuthenticated) {
				contextUser.IsAuthenticated = true;

				var userId = UserManager.GetUserId(currentPrincipal);
				contextUser.ApplicationUser = DbContext.Users.SingleOrDefault(u => u.Id == userId);

				// Can occur if a user was logged in when their account was deleted from the database.
				if (contextUser.ApplicationUser is null) {
					SignInManager.SignOutAsync().ConfigureAwait(false);
					contextUser.IsAuthenticated = false;
					return contextUser;
				}

				var userRolesQuery = from userRole in DbContext.UserRoles
									 join role in DbContext.Roles on userRole.RoleId equals role.Id
									 where userRole.UserId.Equals(contextUser.ApplicationUser.Id)
									 select role.Name;

				contextUser.Roles = userRolesQuery.ToList();

				var adminRole = DbContext.Roles.SingleOrDefault(r => r.Name == "Admin");

				var adminUsersQuery = from user in DbContext.Users
									  join userRole in DbContext.UserRoles on user.Id equals userRole.UserId
									  join role in DbContext.Roles on userRole.RoleId equals role.Id
									  where role.Name == "Admin"
									  select user.Id;

				var adminUsers = adminUsersQuery.ToList();

				// Occurs when there is no admin role created yet.
				if (adminRole is null) {
					contextUser.IsAdmin = true;
				}
				// Occurs when there is an admin role, but no admin users yet.
				else if (adminUsers.Count() == 0) {
					contextUser.IsAdmin = true;
				}
				else if (contextUser.Roles.Contains("Admin")) {
					// Force logout if the user was removed from Admin, but their session still says they're in Admin.
					if (!adminUsersQuery.Any(uid => uid == contextUser.ApplicationUser.Id)) {
						contextUser.IsAdmin = false;
						SignInManager.SignOutAsync().ConfigureAwait(false);
						return contextUser;
					}

					contextUser.IsAdmin = true;
				}

				contextUser.ApplicationUser.LastOnline = DateTime.Now;

				DbContext.Update(contextUser.ApplicationUser);
				DbContext.SaveChanges();
			}

			return contextUser;
		}
	}
}