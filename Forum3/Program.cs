using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace Forum3 {
	public class Program {
		public static void Main(string[] args) {
			var host = new WebHostBuilder()
				.UseKestrel()
				.UseUrls("http://*:31415")
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.Build();

			host.Run();
		}
	}
}