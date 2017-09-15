using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Forum3.Annotations;
using Forum3.Controllers;
using Forum3.Helpers;
using Forum3.Models.DataModels;

namespace Forum3 {
	public class Startup {
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			// Loads from the user-secrets store
			var dbConnectionString = Configuration["DefaultConnection"];

			// Or use the one defined in appsettings.json
			if (string.IsNullOrEmpty(dbConnectionString))
				dbConnectionString = Configuration.GetConnectionString("DefaultConnection");

			services.AddDbContext<ApplicationDbContext>(options =>
				options.UseSqlServer(dbConnectionString)
			);

			services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
				options.Password.RequireDigit = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequiredLength = 3;
				options.SignIn.RequireConfirmedEmail = true;
			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			services.ConfigureApplicationCookie(options => options.LoginPath = $"/{nameof(Authentication)}/{nameof(Authentication.Login)}");

			services.Configure<MvcOptions>(options => {
				options.Filters.Add(new RequireRemoteHttpsAttribute());
			});

			services.AddForum(Configuration);

			services.AddDistributedMemoryCache();
			services.AddSession();

			services.AddMvc();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
				app.UseDatabaseErrorPage();
			}
			else {
				app.UseExceptionHandler("/Boards/Error");
			}

			app.UseStaticFiles();

			app.UseAuthentication();

			app.UseSession();

			app.UseMvc(routes => {
				routes.MapRoute(
					name: "default",
					template: "{controller=Boards}/{action=Index}/{id?}/{pageId?}/{target?}");
			});
		}
	}
}