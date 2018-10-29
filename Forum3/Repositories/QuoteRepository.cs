using Forum3.Contexts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Forum3.Repositories {
	using DataModels = Models.DataModels;
	using ViewModels = Models.ViewModels;

	public class QuoteRepository : Repository<DataModels.Quote> {
		ApplicationDbContext DbContext { get; }
		AccountRepository AccountRepository { get; }

		public QuoteRepository(
			ILogger<PinRepository> log,
			ApplicationDbContext dbContext,
			AccountRepository accountRepository
		) : base(log) {
			DbContext = dbContext;
			AccountRepository = accountRepository;
		}

		public ViewModels.Sidebar.Quote Get() {
			if (!Records.Any()) {
				// return null;
				return new ViewModels.Sidebar.Quote {
					Id = 1,
					PostedBy = "Cheschire",
					Body = @"Lorem ipsum dolor sit amet,
							consectetur adipiscing elit.
							Donec facilisis eleifend
							rutrum. Integer scelerisque
							eros bibendum risus aliquet
							dignissim. Mauris consectetur
							faucibus neque. Nullam justo
							mauris, commodo vel feugiat
							ut, pulvinar quis leo. Etiam
							sem augue, tristique eu
							commodo eu, pulvinar id
							justo. Cras pharetra libero
							sit amet facilisis lacinia.
							Mauris magna risus, sodales
							a bibendum at, aliquam vitae
							leo."
				};
			}

			var randomQuoteIndex = new Random().Next(0, Records.Count);
			var randomQuote = Records[randomQuoteIndex];

			var postedBy = AccountRepository.FirstOrDefault(r => r.Id == randomQuote.PostedById);

			return new ViewModels.Sidebar.Quote {
				Id = randomQuote.MessageId,
				Body = randomQuote.Body,
				PostedBy = postedBy.DisplayName
			};
		}

		protected override List<DataModels.Quote> GetRecords() => DbContext.Quotes.ToList();
	}
}
