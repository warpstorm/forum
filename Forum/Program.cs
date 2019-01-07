using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forum {
	public class Program {
		public static void Main(string[] args) => BuildWebHost(args).Run();

		public static IWebHost BuildWebHost(string[] args) => WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.ConfigureAppConfiguration((builderContext, config) => {
					config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
						  .AddJsonFile($"appsettings.{builderContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
				})
				.ConfigureLogging((hostingContext, logging) => {
					logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
					logging.AddDebug();
					logging.AddAzureWebAppDiagnostics();
				})
				.Build();
	}
}
