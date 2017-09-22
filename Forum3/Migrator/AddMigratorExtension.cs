using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum3.Migrator {
	public static class AddMigratorExtension {
		public static IServiceCollection AddMigrator(this IServiceCollection services, IConfiguration configuration) {
			services.AddScoped((serviceProvider) => {
				var connectionString = configuration["Version2Connection"];
				return new Models.MigrationDbContext(connectionString);
			});

			services.AddScoped<MigratorService>();

			return services;
		}
	}
}