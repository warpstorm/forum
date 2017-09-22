using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using DataModels = Forum3.Models.DataModels;
using MigrationModels = Forum3.Migrator.Models;

namespace Forum3.Migrator {
	public static class MigratorExtension {
		public static IServiceCollection AddMigrator(this IServiceCollection services, IConfiguration configuration) {
			services.AddScoped((serviceProvider) => {
				var connectionString = configuration["Version2Connection"];
				return new MigrationModels.MigrationDbContext(connectionString);
			});

			services.AddScoped<MigratorService>();

			return services;
		}
	}

	public class MigratorService {
		DataModels.ApplicationDbContext ApplicationDbContext { get; }
		MigrationModels.MigrationDbContext MigrationDbContext { get; }
		RoleManager<DataModels.ApplicationRole> RoleManager { get; }
		UserManager<DataModels.ApplicationUser> UserManager { get; }

		public MigratorService(
			DataModels.ApplicationDbContext applicationDbContext,
			MigrationModels.MigrationDbContext migrationDbContext,
			RoleManager<DataModels.ApplicationRole> roleManager,
			UserManager<DataModels.ApplicationUser> userManager
		) {
			ApplicationDbContext = applicationDbContext;
			MigrationDbContext = migrationDbContext;
			RoleManager = roleManager;
			UserManager = userManager;
		}

		public bool Test() {
			var test = MigrationDbContext.Messages.Where(m => m.TimePosted > DateTime.Now.AddDays(-1)).ToList();

			if (test.Any())
				return true;

			return false;
		}

		public async Task<bool> Execute() {
			if (!ApplicationDbContext.Users.Any())
				await MigrateUsers();

			if (!ApplicationDbContext.Roles.Any())
				await MigrateRoles();

			if (!ApplicationDbContext.Boards.Any())
				await MigrateBoards();

			return true;
		}

		async Task MigrateUsers() {
			var query = from user in MigrationDbContext.UserProfiles
						join membership in MigrationDbContext.Membership on user.UserId equals membership.UserId
						select new DataModels.ApplicationUser {
							LegacyId = user.UserId,
							PasswordHash = membership.Password,
							DisplayName = user.DisplayName,
							Birthday = user.Birthday,
							Email = user.UserName,
							EmailConfirmed = true,
							NormalizedEmail = user.UserName.ToUpper(),
							NormalizedUserName = user.UserName.ToUpper(),
							UserName = user.UserName,
							Registered = user.Registered,
							LastOnline = user.LastOnline,
						};

			var records = await query.ToListAsync();

			foreach (var record in records)
				await UserManager.CreateAsync(record);
		}

		async Task MigrateBoards() {
			var category = new DataModels.Category {
				Name = "Migration Category",
				DisplayOrder = 100
			};

			await ApplicationDbContext.AddAsync(category);
			await ApplicationDbContext.SaveChangesAsync();

			var query = from board in MigrationDbContext.Boards
						select new DataModels.Board {
							LegacyId = board.Id,
							CategoryId = category.Id,
							DisplayOrder = board.DisplayOrder,
							Name = board.Name
						};

			var records = await query.ToListAsync();

			foreach (var record in records)
				await ApplicationDbContext.AddAsync(record);

			await ApplicationDbContext.SaveChangesAsync();
		}

		async Task MigrateRoles() {
			var usersInRolesRecords = await MigrationDbContext.UsersInRoles.ToListAsync();
			var rolesRecords = await MigrationDbContext.Roles.ToListAsync();

			var query = from user in ApplicationDbContext.Users
						join userInRole in usersInRolesRecords on user.LegacyId equals userInRole.UserId
						join role in rolesRecords on userInRole.RoleId equals role.Id
						select new {
							User = user,
							RoleName = role.Name
						};

			var records = await query.ToListAsync();

			foreach (var record in records) {
				if (!await RoleManager.RoleExistsAsync(record.RoleName)) {
					await RoleManager.CreateAsync(new DataModels.ApplicationRole {
						Name = record.RoleName
					});
				}

				await UserManager.AddToRoleAsync(record.User, record.RoleName);
			}
		}

		// Unfinished
		async Task MigrateSmileys() {
			var records = await MigrationDbContext.Smileys.ToListAsync();

			foreach (var record in records) {
				var column = Convert.ToInt32(1000 * Math.Floor(record.DisplayOrder));
				var row = Convert.ToInt32(100 * (record.DisplayOrder - column));

				var newSmiley = new DataModels.Smiley {
					Code = record.Code,
					FileName = record.Path,
					Thought = record.Thought,
					SortOrder = column + row,
				};

				await ApplicationDbContext.AddAsync(newSmiley);
			}

			await ApplicationDbContext.SaveChangesAsync();
		}
	}
}