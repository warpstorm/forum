using Forum3.Contexts;
using Forum3.Controllers;
using Forum3.Extensions;
using Forum3.Interfaces;
using Forum3.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Linq;

namespace Forum3.Processes {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class RebuildThreadRelationshipsProcess : IControllerProcess {
		ApplicationDbContext DbContext { get; }
		SettingsRepository Settings { get; }
		IUrlHelper UrlHelper { get; }

		public RebuildThreadRelationshipsProcess(
			ApplicationDbContext dbContext,
			SettingsRepository settingsRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			Settings = settingsRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		public ViewModels.Delay Start() {
			var parentMessageQuery = from message in DbContext.Messages
									 where message.LegacyParentId == 0
									 where message.ParentId == 0
									 orderby message.Id descending
									 select message;

			var recordCount = parentMessageQuery.Count();

			var take = Settings.MessagesPerPage();

			return ViewModel(new InputModels.Continue {
				Stage = nameof(Continue),
				CurrentStep = -1,
				TotalSteps = Convert.ToInt32(Math.Ceiling(1D * recordCount / take))
			});
		}

		public ViewModels.Delay Continue(InputModels.Continue input) {
			input.ThrowIfNull(nameof(input));

			var historyTimeLimit = Settings.HistoryTimeLimit();
			var take = Settings.MessagesPerPage();
			var skip = take * input.CurrentStep;

			var parentMessageQuery = from message in DbContext.Messages
									 where message.LegacyParentId == 0
									 where message.ParentId == 0
									 orderby message.Id descending
									 select message;

			foreach (var parentMessage in parentMessageQuery.Skip(skip).Take(take)) {
				var childMessagesQuery = from message in DbContext.Messages
										 where message.ParentId == parentMessage.Id || (parentMessage.LegacyId != 0 && message.LegacyParentId == parentMessage.LegacyId)
										 select message;

				var lastReply = new DataModels.Message {
					Id = -1
				};

				var replyCount = 0;

				foreach (var childMessage in childMessagesQuery) {
					var replyMessage = DbContext.Messages.FirstOrDefault(r => r.LegacyId == childMessage.LegacyReplyId);

					childMessage.ParentId = parentMessage.Id;
					childMessage.ReplyId = replyMessage?.Id ?? 0;

					DbContext.Update(childMessage);

					if (childMessage.Id > lastReply.Id)
						lastReply = childMessage;

					replyCount++;
				}

				if (lastReply.Id > 0) {
					parentMessage.LastReplyId = lastReply.Id;
					parentMessage.LastReplyPosted = lastReply.TimePosted;
					parentMessage.LastReplyById = lastReply.PostedById;
				}

				parentMessage.ReplyCount = replyCount;

				DbContext.Update(parentMessage);
			}

			DbContext.SaveChanges();

			return ViewModel(input);
		}

		public ViewModels.Delay ViewModel(InputModels.Continue input) {
			var viewModel = new ViewModels.Delay {
				ActionName = "Rebuilding thread relationships",
				ActionNote = "Connecting replies to their parents.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Topics.Admin), nameof(Topics))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Topics.RebuildThreadRelationships), nameof(Topics), input);
			}

			return viewModel;
		}
	}
}