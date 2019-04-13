using Forum.Controllers.Annotations;
using Forum.Models.Errors;
using Forum.Services;
using Forum.Services.Contexts;
using Forum.Services.Helpers;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Controllers {
	using ViewModels = Models.ViewModels.Notifications;

	public class Notifications : Controller {
		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		NotificationRepository NotificationRepository { get; }
		IForumViewResult ForumViewResult { get; }

		public Notifications(
			ApplicationDbContext dbContext,
			UserContext userContext,
			NotificationRepository notificationRepository,
			IForumViewResult forumViewResult
		) {
			DbContext = dbContext;
			UserContext = userContext;
			NotificationRepository = notificationRepository;
			ForumViewResult = forumViewResult;
		}

		[ActionLog("is viewing their notifications.")]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var showRead = false;

			if (Request.Query.ContainsKey("show-read")) {
				showRead = true;
			}

			var notifications = await NotificationRepository.Index(showRead);

			var viewModel = new ViewModels.Pages.IndexPage {
				Notifications = notifications
			};

			return await ForumViewResult.ViewResult(this, viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Open(int id) {
			if (ModelState.IsValid) {
				var recordQuery = from n in DbContext.Notifications
								  where n.UserId == UserContext.ApplicationUser.Id
								  where n.Id == id
								  select n;

				var record = await recordQuery.FirstOrDefaultAsync();

				if (record is null) {
					throw new HttpNotFoundError();
				}

				if (record.Unread) {
					record.Unread = false;
					DbContext.Update(record);
					await DbContext.SaveChangesAsync();
				}

				var message = await DbContext.Messages.FindAsync(record.MessageId);
				var topicId = message.TopicId;

				var redirectPath = Url.Action(nameof(Topics.Display), nameof(Topics), new { id = topicId, page = 1, target = record.MessageId }) + $"#message{record.MessageId}";
				return Redirect(redirectPath);
			}

			return ForumViewResult.RedirectToReferrer(this);
		}

		[HttpGet]
		public ActionResult MarkAllRead() => Redirect("/");
	}
}