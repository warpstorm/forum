using Forum.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels;

	public class Home : Controller {
		ForumViewResult ForumViewResult { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		IAntiforgery Xsrf { get; }

		public Home(
			ForumViewResult forumViewResult,
			IHttpContextAccessor httpContextAccessor,
			IAntiforgery xsrf
		) {
			ForumViewResult = forumViewResult;
			HttpContextAccessor = httpContextAccessor;
			Xsrf = xsrf;
		}

		[AllowAnonymous]
		public IActionResult Error() {
			var viewModel = new ViewModels.Error {
				RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public IActionResult WhosOnline() => ForumViewResult.ViewResult(this, "Components/OnlineUsersList/_Partial");

		[HttpGet]
		public IActionResult Token() => Json(new {
			token = Xsrf.GetAndStoreTokens(HttpContextAccessor.HttpContext).RequestToken
		});
	}
}