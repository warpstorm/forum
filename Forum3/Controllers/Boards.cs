using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Services;
using System.Threading.Tasks;

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
		public async Task<IActionResult> Index() {
			var viewModel = await BoardService.GetBoardIndex();

			return View("Index", viewModel);
		}
	}
}