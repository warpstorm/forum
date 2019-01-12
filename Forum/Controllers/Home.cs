using Forum.Contexts;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

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
		public async Task<IActionResult> Error() {
			var viewModel = new ViewModels.Error {
				RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> WhosOnline() {
			var viewModel = await AccountRepository.GetOnlineList();
			return await ForumViewResult.ViewResult(this, "Sidebar/_OnlineUsersList", viewModel);
		}

		[HttpGet]
		public IActionResult Token() => Json(new {
			token = Xsrf.GetAndStoreTokens(HttpContextAccessor.HttpContext).RequestToken
		});
	}
}