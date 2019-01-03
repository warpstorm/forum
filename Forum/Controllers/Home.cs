using Forum.Contexts;
using Forum.Enums;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels;

	public class Home : Controller {
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		IForumViewResult ForumViewResult { get; }
		IHttpContextAccessor HttpContextAccessor { get; }
		IAntiforgery Xsrf { get; }

		public Home(
			UserContext userContext,
			AccountRepository accountRepository,
			IForumViewResult forumViewResult,
			IHttpContextAccessor httpContextAccessor,
			IAntiforgery xsrf
		) {
			UserContext = userContext;
			AccountRepository = accountRepository;
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
		public IActionResult WhosOnline() {
			var viewModel = AccountRepository.GetOnlineList();

			ViewData[Constants.InternalKeys.Layout] = "_LayoutEmpty";

			return ForumViewResult.ViewResult(this, "Sidebar/_OnlineUsersList", viewModel);
		}

		[HttpGet]
		public IActionResult Token() => Json(new {
			token = Xsrf.GetAndStoreTokens(HttpContextAccessor.HttpContext).RequestToken
		});
	}
}