using System;
using System.Linq;
using Forum3.Models.MigrationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum3.Services {
	public static class MigratorExtension {
		public static IServiceCollection AddMigrator(this IServiceCollection services, IConfiguration configuration) {
			services.AddScoped((serviceProvider) => {
				var connectionString = configuration["Version2Connection"];
				var migrationDbContext = new MigrationDbContext(connectionString);
				return new MigratorService(migrationDbContext);
			});

			return services;
		}
	}

	public class MigratorService {
		MigrationDbContext MigrationDbContext { get; }

		public MigratorService(MigrationDbContext migrationDbContext) {
			MigrationDbContext = migrationDbContext;
		}

		public bool Test() {
			var test = MigrationDbContext.Messages.Where(m => m.TimePosted > DateTime.Now.AddDays(-1)).ToList();

			if (test.Any())
				return true;

			return false;
		}
	}
}