using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Forum3.Data;
using Forum3.Helpers;
using Forum3.Annotations;
using Forum3.Models.DataModels;

namespace Forum3 {
	public class Startup {
		public Startup(IHostingEnvironment env) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

			if (env.IsDevelopment()) {
				builder.AddUserSecrets<Startup>();
			}

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			// Uncomment to use the DefaultConnection string in appsettings.json
			//var connectionString = Configuration.GetConnectionString("DefaultConnection");

			// Loads from the user-secrets store
			var connectionString = Configuration["DefaultConnection"];

			// Add framework services.
			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(connectionString));

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

			services.Configure<MvcOptions>(options => {
				options.Filters.Add(new RequireRemoteHttpsAttribute());
			});

			services.AddMvc();

			services.AddDistributedMemoryCache();
			services.AddSession();
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

			app.UseStaticFiles();

			app.UseIdentity();

			app.UseSession();

			app.UseMvc(routes => {
				routes.MapRoute(
					name: "default",
					template: "{controller=Topics}/{action=Index}/{id?}/{page?}/{target?}");
			});
		}
	}
}
