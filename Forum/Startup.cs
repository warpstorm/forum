using Forum.Contexts;
using Forum.Controllers;
using Forum.Extensions;
using Forum.Filters;
using Forum.Plugins;
using Forum.Services;
using Jdenticon.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forum {
	using DataModels = Models.DataModels;

	public class Startup {
		public IConfiguration Configuration { get; }

		public Startup(
			IConfiguration configuration
		) {
			Configuration = configuration;
		}

		public void ConfigureServices(IServiceCollection services) {
			services.AddDbContextPool<ApplicationDbContext>(
				options => options.UseSqlServer(GetDbConnectionString())
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

				var policy = new AuthorizationPolicyBuilder()
				 .RequireAuthenticatedUser()
				 .Build();

				options.Filters.Add(new AuthorizeFilter(policy));
			});

			services.AddForum(Configuration);
			services.AddPlugins(Configuration);

			services.AddDistributedMemoryCache();
			services.AddSession();
			services.AddSignalR();

			services.AddMvc(options => options.EnableEndpointRouting = false)
				.SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseBrowserLink();
				app.UseDatabaseErrorPage();
			}
			else {
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseJdenticon();
			app.UseStaticFiles();
			app.UseAuthentication();
			app.UseSession();
			app.UseForum();

			app.UseSignalR(routes => {
				routes.MapHub<ForumHub>("/Hub");
			});

			app.UseMvc(routes => {
				routes.Routes.Add(new ForumRouter(app, routes.DefaultHandler));

				routes.MapRoute(
					name: "default",
					template: "{controller}/{action}/{id?}/{pageId?}/{target?}");
			});
		}

		string GetDbConnectionString() {
			// Loads from the environment
			var dbConnectionString = Configuration["DefaultConnection"];

			// Or use the one defined in ConnectionStrings setting of app configuration.
			if (string.IsNullOrEmpty(dbConnectionString)) {
				dbConnectionString = Configuration.GetConnectionString("DefaultConnection");
			}

			return dbConnectionString;
		}
	}
}