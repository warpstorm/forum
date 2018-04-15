using Forum3.Contexts;
using Forum3.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using ViewModels = Models.ViewModels.Profile;

	public class Profile : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		IForumViewResult ForumViewResult { get; }
		UrlEncoder UrlEncoder { get; }

		public Profile(
			ApplicationDbContext dbContext,
			UserContext userContext,
			IForumViewResult forumViewResult,
			UrlEncoder urlEncoder
		) {
			DbContext = dbContext;
			UserContext = userContext;
			ForumViewResult = forumViewResult;
			UrlEncoder = urlEncoder;
		}
		
		[HttpGet]
		public async Task<IActionResult> Details(string id) {
			if (string.IsNullOrEmpty(id))
				id = UserContext.ApplicationUser.Id;

			var userRecord = await DbContext.Users.FindAsync(id);

			if (userRecord is null)
				throw new Exception($"No record found with the id {id}");

			// TODO check access rights i.e trim email

			var viewModel = new ViewModels.DetailsPage {
				Id = userRecord.Email,
				DisplayName = userRecord.DisplayName,
				Email = userRecord.Email,
			};

			return ForumViewResult.ViewResult(this, viewModel);
		}
	}
}