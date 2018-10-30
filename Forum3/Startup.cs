using Forum3.Filters;
using Forum3.Contexts;
using Forum3.Controllers;
using Forum3.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Jdenticon.AspNetCore;

namespace Forum3 {
	using DataModels = Models.DataModels;

	public class Startup {
		public IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration) {
			Configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services) {
			// services.AddApplicationInsightsTelemetry(Configuration);

			// Loads from the environment
			var dbConnectionString = Configuration["DefaultConnection"];

			// Or use the one defined in ConnectionStrings setting of app configuration.
			if (string.IsNullOrEmpty(dbConnectionString))
				dbConnectionString = Configuration.GetConnectionString("DefaultConnection");

			services.AddDbContextPool<ApplicationDbContext>(options =>
				options.UseSqlServer(dbConnectionString)
			);

			services.AddIdentity<DataModels.ApplicationUser, DataModels.ApplicationRole>(options => {
				options.Password.RequireDigit = false;
				options.Password.RequireLowercase = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.Password.RequiredLength = 3;
				options.SignIn.RequireConfirmedEmail = true;
			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			services.ConfigureApplicationCookie(options => options.LoginPath = $"/{nameof(Account)}/{nameof(Account.Login)}");

			services.Configure<MvcOptions>(options => {
				//options.Filters.Add<RequireRemoteHttpsAttribute>();
				options.Filters.Add<UserContextActionFilter>();
			});

			services.AddForum(Configuration);

			services.AddDistributedMemoryCache();
			services.AddSession();

			services.AddMvc(config => {
				var policy = new AuthorizationPolicyBuilder()
								 .RequireAuthenticatedUser()
								 .Build();

				config.Filters.Add(new AuthorizeFilter(policy));
			}).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
				app.UseDatabaseErrorPage();
			}
			else
				app.UseExceptionHandler("/Home/Error");

			app.UseJdenticon();
			app.UseStaticFiles();
			app.UseAuthentication();
			app.UseSession();
			app.UseForum();

			app.UseMvc(routes => {
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=FrontPage}/{id?}/{pageId?}/{target?}");
			});
		}
	}
}