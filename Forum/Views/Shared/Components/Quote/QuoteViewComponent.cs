using Forum.Services.Contexts;
using Forum.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Views.Shared.Components.Quote {
	public class QuoteViewComponent : ViewComponent {
		ApplicationDbContext DbContext { get; }
		AccountRepository AccountRepository { get; }
		QuoteRepository QuoteRepository { get; }

		public QuoteViewComponent(
			ApplicationDbContext dbContext,
			AccountRepository accountRepository,
			QuoteRepository quoteRepository
		) {
			DbContext = dbContext;
			AccountRepository = accountRepository;
			QuoteRepository = quoteRepository;
		}

		public async Task<IViewComponentResult> InvokeAsync() {
			var records = await QuoteRepository.Records();
			var approvedRecords = records.Where(r => r.Approved).ToList();

			DisplayItem viewModel = null;

			if (approvedRecords.Any()) {
				var randomQuoteIndex = new Random().Next(0, approvedRecords.Count);
				var randomQuote = approvedRecords[randomQuoteIndex];

				var postedBy = (await AccountRepository.Records()).FirstOrDefault(r => r.Id == randomQuote.PostedById);
				var message = DbContext.Messages.Find(randomQuote.MessageId);

				viewModel = new DisplayItem {
					TopicId = message.TopicId,
					MessageId = randomQuote.MessageId,
					DisplayBody = randomQuote.DisplayBody,
					PostedBy = postedBy.DecoratedName
				};

			}

			return View("Default", viewModel);
		}

		public class DisplayItem {
			public int TopicId { get; set; }
			public int MessageId { get; set; }
			public string DisplayBody { get; set; }
			public string PostedBy { get; set; }
		}
	}
}
