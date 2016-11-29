using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Services;

namespace Forum3.Controllers {
	[Authorize(Roles = "Admin")]
	[RequireRemoteHttps]
	public class Boards : Controller {
		public BoardService BoardService { get; }
		public TopicService TopicService { get; }
		public MessageService MessageService { get; }

		public Boards(BoardService boardService, TopicService topicService, MessageService messageService) {
			BoardService = boardService;
			TopicService = topicService;
			MessageService = messageService;
		}

		[AllowAnonymous]
		public ActionResult Index() {
			var boards = BoardTree.Create(Db.Boards.ToList(), Db);
			var onlineUsers = OnlineUsers.Load(Db);

			var birthdays = Db.UserProfiles.Select(u => new Birthday {
				Date = u.Birthday,
				DisplayName = u.DisplayName
			}).ToList();

			var todayBirthdayNames = new List<string>();

			if (birthdays.Count > 0) {
				var todayBirthdays = birthdays.Where(u => new DateTime(DateTime.Now.Year, u.Date.Month, u.Date.Day).Date == DateTime.Now.Date);

				foreach (var item in todayBirthdays) {
					DateTime now = DateTime.Today;
					int age = now.Year - item.Date.Year;
					if (item.Date > now.AddYears(-age)) age--;

					todayBirthdayNames.Add(item.DisplayName + " (" + age + ")");
				}
			}

			var viewModel = new Models.View.Boards.Index {
				Birthdays = todayBirthdayNames.ToArray(),
				Boards = boards,
				OnlineUsers = onlineUsers
			};

			return View("Index", viewModel);
		}
	}
}