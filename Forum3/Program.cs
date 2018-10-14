using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Forum3 {
	public class Program {
		public static void Main(string[] args) => BuildWebHost(args).Run();

		public static IWebHost BuildWebHost(string[] args) {
			return WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.ConfigureAppConfiguration((builderContext, config) => {
					config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
						  .AddJsonFile($"appsettings.{builderContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
				})
				.Build();
		}
	}
}
