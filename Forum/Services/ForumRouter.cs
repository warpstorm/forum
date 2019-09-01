using Forum.Services.Contexts;
using Forum.Controllers;
using Forum.Models.Options;
using Forum.Models.DataModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Forum.Services {
	public class ForumRouter : IRouter {
		IApplicationBuilder Builder { get; }
		IRouter DefaultRouter { get; }

		public ForumRouter(
			IApplicationBuilder builder,
			IRouter defaultRouter
		) {
			Builder = builder;
			DefaultRouter = defaultRouter;
		}

		/// <summary>
		/// Used internally by MVC. Nothing custom here.
		/// </summary>
		public VirtualPathData GetVirtualPath(VirtualPathContext context) => DefaultRouter.GetVirtualPath(context);

		public async Task RouteAsync(RouteContext context) {
			var atFrontPage = context.HttpContext.Request.Path.Value == "/";

			if (atFrontPage) {
				ApplicationUser user = null;
				var frontPage = EFrontPage.Boards;

				if (context.HttpContext.User.Identity.IsAuthenticated) {
					using var serviceScope = Builder.ApplicationServices.CreateScope();

					var id = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
					var dbContext = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();

					user = dbContext.Users.FirstOrDefault(r => r.Id == id);

					if (!(user is null)) {
						frontPage = user.FrontPage;
					}
				}

				switch (frontPage) {
					default:
					case EFrontPage.Boards:
						context.RouteData.Values["controller"] = nameof(Boards);
						context.RouteData.Values["action"] = nameof(Boards.Index);
						break;

					case EFrontPage.All:
						context.RouteData.Values["controller"] = nameof(Topics);
						context.RouteData.Values["action"] = nameof(Topics.Index);
						break;

					case EFrontPage.Unread:
						context.RouteData.Values["controller"] = nameof(Topics);
						context.RouteData.Values["action"] = nameof(Topics.Index);
						context.RouteData.Values["id"] = 0;
						context.RouteData.Values["unread"] = 1;
						break;
				}
			}

			await DefaultRouter.RouteAsync(context);
		}
	}
}
