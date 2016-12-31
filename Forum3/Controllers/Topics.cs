using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum3.Annotations;
using Forum3.Services;
using Forum3.ViewModels.Topics.Items;
using Forum3.InputModels;

namespace Forum3.Controllers {
	[Authorize]
	[RequireRemoteHttps]
	public class Topics : Controller {
		public TopicService TopicService { get; }
		public MessageService MessageService { get; }

		public Topics(TopicService topicService, MessageService messageService) {
			TopicService = topicService;
			MessageService = messageService;
		}

		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> Index() {
			var skip = 0;

			if (Request.Query.ContainsKey("skip"))
				skip = Convert.ToInt32(Request.Query["skip"]);

			var take = 15;

			if (Request.Query.ContainsKey("take"))
				take = Convert.ToInt32(Request.Query["take"]);

			var viewModel = await TopicService.GetTopicIndex(skip, take);

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Display(int id, int page = 1) {
			var jumpToLatest = false;

			if (page == 0) {
				page = 1;
				jumpToLatest = true;
			}

			var take = 15;
			var skip = (page * take) - take;

			var message = MessageService.Find(id);

			if (message.ParentId > 0) {
				var actionUrl = Url.Action(nameof(Display), new { id = message.ParentId });
				return new RedirectResult(actionUrl + "#message" + id);
			}

			var viewModel = await TopicService.GetTopic(message, page, skip, take, jumpToLatest);

			return View(viewModel);
		}

		[HttpGet]
		public IActionResult Create() {
			return View(new TopicFirstPost());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MessageInput input) {
			if (ModelState.IsValid) {
				await MessageService.Create(input.Body);
				return RedirectToAction(nameof(Index), nameof(Topics));
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(EditPost input) {
			if (ModelState.IsValid) {
				await MessageService.Update(input.Id, input.Body);
				return RedirectToAction(nameof(Display), nameof(Topics), new { Id = input.Id });
			}

			// TODO - replace this by returning back to the Display view, or doing web api.
			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> TopicReply(TopicReplyPost input) {
			if (ModelState.IsValid) {
				await MessageService.Create(input.Body, input.Id);
				return RedirectToAction(nameof(Display), nameof(Topics), new { Id = input.Id });
			}

			return View(input);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DirectReply(DirectReplyPost input) {
			if (ModelState.IsValid) {
				await MessageService.Create(input.Body, input.Id);
				return RedirectToAction(nameof(Display), new { Id = input.Id });
			}

			return View(input);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			await MessageService.Delete(id);
			return RedirectToAction(nameof(Index), nameof(Topics));
		}
	}
}