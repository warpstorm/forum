using Forum.Contexts;
using Forum.Interfaces.Services;
using Forum.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels.Profile;

	public class Profile : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		IForumViewResult ForumViewResult { get; }
		UrlEncoder UrlEncoder { get; }

		public Profile(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			IForumViewResult forumViewResult,
			UrlEncoder urlEncoder
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			ForumViewResult = forumViewResult;
			UrlEncoder = urlEncoder;
		}

		[HttpGet]
		public async Task<IActionResult> Details(string id) {
			if (string.IsNullOrEmpty(id)) {
				id = UserContext.ApplicationUser.Id;
			}

			var userRecord = (await AccountRepository.Records()).First(item => item.Id == id);

			var viewModel = new ViewModels.DetailsPage {
				Id = userRecord.Email,
				DisplayName = userRecord.DisplayName,
				Email = userRecord.Email,
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}
	}
}