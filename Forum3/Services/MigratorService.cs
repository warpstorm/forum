using System;
using System.Linq;
using System.Threading.Tasks;
using Forum3.Models.DataModels;
using Forum3.Models.MigrationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum3.Services {
	public static class MigratorExtension {
		public static IServiceCollection AddMigrator(this IServiceCollection services, IConfiguration configuration) {
			services.AddScoped((serviceProvider) => {
				var connectionString = configuration["Version2Connection"];
				return new MigrationDbContext(connectionString);
			});

			services.AddScoped<MigratorService>();

			return services;
		}
	}

	public class MigratorService {
		ApplicationDbContext ApplicationDbContext { get; }
		MigrationDbContext MigrationDbContext { get; }

		public MigratorService(
			ApplicationDbContext applicationDbContext,
			MigrationDbContext migrationDbContext
		) {
			ApplicationDbContext = applicationDbContext;
			MigrationDbContext = migrationDbContext;
		}

		public bool Test() {
			var test = MigrationDbContext.Messages.Where(m => m.TimePosted > DateTime.Now.AddDays(-1)).ToList();

			if (test.Any())
				return true;

			return false;
		}

		public async Task<bool> MigrateUsers() {
			var userQuery = from user in MigrationDbContext.UserProfiles
							join membership in MigrationDbContext.Membership on user.UserId equals membership.UserId
							select new ApplicationUser {
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

			var users = await userQuery.ToListAsync();

			foreach (var user in users)
				await ApplicationDbContext.AddAsync(user);

			await ApplicationDbContext.SaveChangesAsync();

			return true;
		}
	}
}