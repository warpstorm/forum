using Forum.Contexts;
using Forum.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forum.Repositories {
	using DataModels = Models.DataModels;
	using InputModels = Models.InputModels;
	using ServiceModels = Models.ServiceModels;
	using ViewModels = Models.ViewModels;

	public class QuoteRepository : IRepository<DataModels.Quote> {
		public async Task<List<DataModels.Quote>> Records() => _Records ?? (_Records = await DbContext.Quotes.ToListAsync());
		List<DataModels.Quote> _Records;

		ApplicationDbContext DbContext { get; }
		UserContext UserContext { get; }
		AccountRepository AccountRepository { get; }
		MessageRepository MessageRepository { get; }

		public QuoteRepository(
			ApplicationDbContext dbContext,
			UserContext userContext,
			AccountRepository accountRepository,
			MessageRepository messageRepository,
			ILogger<PinRepository> log
		) {
			DbContext = dbContext;
			UserContext = userContext;
			AccountRepository = accountRepository;
			MessageRepository = messageRepository;
		}

		public async Task<ViewModels.Quotes.EditQuotes> Index() {
			var returnObject = new ViewModels.Quotes.EditQuotes {
				Quotes = new List<ViewModels.Quotes.EditQuote>()
			};

			foreach (var record in await Records()) {
				var originalMessage = DbContext.Messages.FirstOrDefault(r => r.Id == record.MessageId);
				var postedBy = (await AccountRepository.Records()).FirstOrDefault(r => r.Id == record.PostedById);
				var submittedBy = (await AccountRepository.Records()).FirstOrDefault(r => r.Id == record.SubmittedById);

				returnObject.Quotes.Add(new ViewModels.Quotes.EditQuote {
					Id = record.Id,
					MessageId = record.MessageId,
					OriginalBody = record.OriginalBody,
					DisplayBody = record.DisplayBody,
					PostedBy = postedBy?.DisplayName ?? "Missing User",
					PostedTime = originalMessage.TimePosted,
					SubmittedBy = submittedBy?.DisplayName ?? "Missing User",
					SubmittedTime = record.SubmittedTime,
					Approved = record.Approved
				});
			}

			return returnObject;
		}

		public async Task<ViewModels.Quotes.DisplayQuote> Get() {
			var approvedRecords = (await Records()).Where(r => r.Approved).ToList();

			if (!approvedRecords.Any()) {
				return null;
			}

			var randomQuoteIndex = new Random().Next(0, approvedRecords.Count);
			var randomQuote = approvedRecords[randomQuoteIndex];

			var postedBy = (await AccountRepository.Records()).FirstOrDefault(r => r.Id == randomQuote.PostedById);

			return new ViewModels.Quotes.DisplayQuote {
				Id = randomQuote.MessageId,
				DisplayBody = randomQuote.DisplayBody,
				PostedBy = postedBy.DisplayName
			};
		}

		public async Task<ServiceModels.ServiceResponse> Create(int messageId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var message = DbContext.Messages.FirstOrDefault(r => r.Id == messageId);

			if (message is null) {
				serviceResponse.Error($@"No record was found with the id '{messageId}'.");
			}

			if ((await Records()).Any(r => r.MessageId == messageId)) {
				serviceResponse.Error($@"A message with the id '{messageId}' has already been submitted.");
			}

			if (serviceResponse.Success) {
				DbContext.Quotes.Add(new DataModels.Quote {
					MessageId = message.Id,
					PostedById = message.PostedById,
					TimePosted = message.TimePosted,
					SubmittedById = UserContext.ApplicationUser.Id,
					SubmittedTime = DateTime.Now,
					Approved = false,
					OriginalBody = message.OriginalBody,
					DisplayBody = message.DisplayBody
				});

				DbContext.SaveChanges();

				serviceResponse.Message = $"Quote added.";
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Edit(InputModels.QuotesInput input) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			foreach (var quoteInput in input.Quotes) {
				var record = (await Records()).FirstOrDefault(r => r.Id == quoteInput.Id);

				if (record is null) {
					serviceResponse.Error($@"No record was found with the id '{quoteInput.Id}'.");
				}

				if (serviceResponse.Success) {
					if (quoteInput.Approved != record.Approved) {
						record.Approved = quoteInput.Approved;
						DbContext.Update(record);
					}

					if (quoteInput.OriginalBody != record.OriginalBody) {
						record.OriginalBody = quoteInput.OriginalBody;

						var processedMessageInput = await MessageRepository.ProcessMessageInput(serviceResponse, quoteInput.OriginalBody);
						record.DisplayBody = processedMessageInput.DisplayBody;

						DbContext.Update(record);
					}

					if (serviceResponse.Success) {
						DbContext.SaveChanges();
						serviceResponse.Message = $"Changes saved.";
					}
				}
			}

			return serviceResponse;
		}

		public async Task<ServiceModels.ServiceResponse> Delete(int quoteId) {
			var serviceResponse = new ServiceModels.ServiceResponse();

			var record = (await Records()).FirstOrDefault(r => r.Id == quoteId);

			if (record is null) {
				serviceResponse.Error($@"No record was found with the id '{quoteId}'.");
			}

			if (serviceResponse.Success) {
				DbContext.Quotes.Remove(record);
				await DbContext.SaveChangesAsync();

				serviceResponse.Message = $"Quote deleted.";
			}

			return serviceResponse;
		}
	}
}
