using Forum.Contexts;
using Forum.Errors;
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
		MessageRepository MessageRepository { get; }
		IForumViewResult ForumViewResult { get; }
		UrlEncoder UrlEncoder { get; }

		public Profile(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			MessageRepository messageRepository,
			IForumViewResult forumViewResult,
			UrlEncoder urlEncoder
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			MessageRepository = messageRepository;
			ForumViewResult = forumViewResult;
			UrlEncoder = urlEncoder;
		}

		[HttpGet]
		public async Task<IActionResult> History(string id, int page = 1) {
			if (string.IsNullOrEmpty(id)) {
				id = UserContext.ApplicationUser.Id;
			}

			var userRecord = (await AccountRepository.Records()).FirstOrDefault(item => item.Id == id);

			if (userRecord is null) {
				throw new HttpNotFoundError();
			}

			var messages = await MessageRepository.GetUserMessages(id, page);
			var morePages = true;

			if (messages.Count < UserContext.ApplicationUser.MessagesPerPage) {
				morePages = false;
			}

			messages = messages.OrderByDescending(r => r.TimePosted).ToList();

			var viewModel = new ViewModels.HistoryPage {
				Id = userRecord.Id,
				DisplayName = userRecord.DisplayName,
				Email = userRecord.Email,
				CurrentPage = page,
				MorePages = morePages,
				ShowFavicons = UserContext.ApplicationUser.ShowFavicons,
				Messages = messages,
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}
	}
}