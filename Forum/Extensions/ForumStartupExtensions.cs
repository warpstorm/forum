using Forum.Core;
using Forum.Data.Contexts;
using Forum.Services;
using Forum.Services.Middleware;
using Forum.Services.Repositories;
using Jdenticon;
using Jdenticon.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

// REMINDER -
// Transient: created each time they are requested. This lifetime works best for lightweight, stateless services.
// Scoped: created once per request.
// Singleton: created the first time they are requested (or when ConfigureServices is run if you specify an instance there) and then every subsequent request will use the same instance.

namespace Forum.Extensions {
	public static class ForumStartupExtensions {
		public static IApplicationBuilder UseForum(this IApplicationBuilder builder) {
			Identicon.DefaultStyle = new IdenticonStyle {
				BackColor = Color.Transparent,
			};

			builder.UseMiddleware<HttpStatusCodeHandler>();
			builder.UseMiddleware<PageTimer>();

			return builder;
		}

		public static IServiceCollection AddForum(this IServiceCollection services) {
			RegisterRepositories(services);

			services.AddScoped<ActionLogService>();
			services.AddScoped<GzipWebClient>();
			services.AddScoped<UserContext>();
			services.AddScoped<UserContextLoader>();

			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
			services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();

			services.AddSingleton((serviceProvider) => {
				return BBCParserFactory.GetParser();
			});

			return services;
		}

		static void RegisterRepositories(IServiceCollection services) {
			services.AddScoped<AccountRepository>();
			services.AddScoped<BoardRepository>();
			services.AddScoped<MessageRepository>();
			services.AddScoped<BookmarkRepository>();
			services.AddScoped<QuoteRepository>();
			services.AddScoped<RoleRepository>();
			services.AddScoped<SmileyRepository>();
			services.AddScoped<TopicRepository>();
		}
	}
}