using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Forum3.Data;
using Forum3.DataModels;
using Forum3.Helpers;

namespace Forum3 {
	public class Startup {
		public Startup(IHostingEnvironment env) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

			if (env.IsDevelopment()) {
				// For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
				builder.AddUserSecrets();
			}

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

			services.AddIdentity<ApplicationUser, IdentityRole>(o => {
				o.Password.RequireDigit = false;
				o.Password.RequireLowercase = false;
				o.Password.RequireUppercase = false;
				o.Password.RequireNonAlphanumeric = false;
				o.Password.RequiredLength = 3;
				o.Cookies.ApplicationCookie.LoginPath = "/Authentication/Login";
			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();
				
			services.AddMvc();
			services.AddForum();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseDatabaseErrorPage();
				app.UseBrowserLink();
			}
			else {
				//app.UseExceptionHandler("/Home/Error");
			}

			app.UseStaticFiles();

			app.UseIdentity();

			app.UseMvc(routes => {
				routes.MapRoute(name: "areaRoute",
					template: "{area:exists}/{controller}/{action}");

				routes.MapRoute(
					name: "default",
					template: "{controller=Topics}/{action=Index}/{id?}");
			});
		}
	}
}
